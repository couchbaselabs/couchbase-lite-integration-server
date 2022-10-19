using Couchbase;
using Couchbase.Core.Exceptions;
using Couchbase.Management.Buckets;
using Couchbase.Management.Collections;
using IntegrationMacroServer.Models;
using IntegrationMacroServer.Models.SyncGateway;
using IntegrationMacroServer.Utility;
using Microsoft.AspNetCore.Mvc;
using Refit;
using System.Diagnostics.Metrics;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text.Json;

namespace IntegrationMacroServer.Controllers
{
    using GetDatabaseResponse = PutDatabaseRequest;

    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class DatabaseController : MacroServerController
    {
        private static readonly ISyncGatewayClient _syncGatewayClient = SyncGateway.Instance;

        public DatabaseController(ILogger<DatabaseController> logger)
            :base(logger)
        {
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DatabaseCreationResult>> CreateDatabase(DatabaseCreation creation)
        {
            var sgUrl = $"http://localhost:4984/{creation.Name}";
            if (!creation.IsValid) {
                return BadRequest(new
                {
                    message = "Invalid input to endpoint"
                });
            }

            var cluster = await CouchbaseCluster.Instance().ConfigureAwait(false);
            try { 
                await cluster.Buckets.CreateBucketAsync(new BucketSettings
                {
                    Name = creation.Bucket,
                    RamQuotaMB = 512,
                    FlushEnabled = true
                }).ConfigureAwait(false);
            } catch(BucketExistsException) {
                Logger.LogInformation("Bucket {0} already exists", creation.Bucket);
            } catch(Exception ex) {
                return new InternalServerErrorResult(new
                {
                    message = ex.Message
                });
            }

            using var bucket = await GetBucket(cluster, creation.Bucket!).ConfigureAwait(false);
            if(bucket == null) {
                return new InternalServerErrorResult(new
                {
                    message = "Unable to get bucket"
                });
            }

            var putDbRequest = new PutDatabaseRequest(creation.Bucket!);
            if (creation.Collections != null) {
                var qualifiedCollections = creation.Collections.Select(c => new QualifiedCollection(c));
                try { 
                    foreach (var c in qualifiedCollections) {
                        await c.CreateIn(bucket).ConfigureAwait(false); 
                        Logger.LogInformation("Sleeping arbitrarily so that server can be ready...");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        Logger.LogInformation("Creating primary index on collection...");
                        await CreatePrimaryIndex(cluster, $"{creation.Bucket}.{c.Scope}.{c.Collection}")
                            .ConfigureAwait(false);
                        Logger.LogInformation("\t...done");
                        var collections = putDbRequest.GetOrAddScope(c.Scope);
                        collections.AddCollection(c.Collection);
                    } 
                } catch (Exception ex) {
                    Logger.LogError(ex, "Exception while creating bucket / collections");
                    return new InternalServerErrorResult(new
                    {
                        message = ex.Message
                    });
                }
            }

            var result = await TryAsync(_syncGatewayClient.PutDatabase, "Creating SGW Database", 
                creation.Name!, putDbRequest).ConfigureAwait(false);
            if(!result.Succeeded) {
                if(result.StatusCode == StatusCodes.Status412PreconditionFailed) {
                    // This means the DB already exists
                    return Ok(new DatabaseCreationResult(sgUrl));
                } 

                return result.Error!;
            }

            if (creation.Username == null) {
                var body = new GuestAccessRequest { Disabled = false };
                body.AdminChannels.Add("*");
                result = await TryAsync(_syncGatewayClient.ManageGuestAccess, "Enabling Guest Access",
                    creation.Name!, body).ConfigureAwait(false);
                if(!result.Succeeded) {
                    return result.Error!;
                }
            } else {
                var body = new CreateUserRequest(creation.Username!, creation.Password!);
                body.AdminChannels.Add("*");
                result = await TryAsync(_syncGatewayClient.ManageGuestAccess, "Disabling Guest Access",
                    creation.Name!, new GuestAccessRequest { Disabled = true }).ConfigureAwait(false);
                if (!result.Succeeded) {
                    return result.Error!;
                }

                result = await TryAsync(_syncGatewayClient.CreateUser, "Creating User",
                    creation.Name!, body).ConfigureAwait(false);
                if (!result.Succeeded) {
                    return result.Error!;
                }
            }

            
            return Created(sgUrl, new DatabaseCreationResult(sgUrl));
        }

        [HttpDelete("{name}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteDatabase(string name, DatabaseDeletion deletion)
        {
            GetDatabaseResponse? dbConfig = null;
            if(deletion.DeleteCollections || deletion.DeleteBucket) {
                var getDbResult = await TryAsyncReturn(_syncGatewayClient.GetDatabase, "Getting info about SGW database",
                    name).ConfigureAwait(false);
                if(!getDbResult.Succeeded) {
                    return getDbResult.Error!;
                } 

                dbConfig = getDbResult.Result;
            }

            var getUsersResult = await TryAsyncReturn(_syncGatewayClient.GetUsers, "getting users",
                name).ConfigureAwait(false);
            if(!getUsersResult.Succeeded) {
                if(getUsersResult.StatusCode == StatusCodes.Status403Forbidden) {
                    // Consider it already gone, admin user shouldn't be forbidden
                    return Ok();
                }
            }

            if(getUsersResult.Result != null) { 
                foreach(var user in getUsersResult.Result) {
                    var deleteUserResult = await TryAsync(_syncGatewayClient.DeleteUser, 
                        $"deleting user {user}", name, user).ConfigureAwait(false);
                    if(!deleteUserResult.Succeeded) {
                        return deleteUserResult.Error!;
                    }
                }
            }

            var deleteDbResult = await TryAsync(_syncGatewayClient.DeleteDatabase, "deleting SGW database",
                name).ConfigureAwait(false);
            if(!deleteDbResult.Succeeded) {
                return deleteDbResult.Error!;
            }

            if(dbConfig == null) {
                return Ok();
            }

            var cb = await CouchbaseCluster.Instance().ConfigureAwait(false);
            if (deletion.DeleteBucket) {
                Logger.LogInformation("Dropping bucket {0}...", dbConfig.Bucket);
                await cb.Buckets.DropBucketAsync(dbConfig.Bucket).ConfigureAwait(false);
                Logger.LogInformation("\t...done");
            } else if(deletion.DeleteCollections) {
                Logger.LogTrace("Getting bucket {0}...", dbConfig.Bucket);
                var bucket = await cb.BucketAsync(dbConfig.Bucket).ConfigureAwait(false);
                Logger.LogTrace("\t...done");
                foreach(var scope in dbConfig.Scopes) {
                    foreach(var coll in scope.Value.Collections) {
                        Logger.LogInformation("Dropping collection '{0}.{1}' from '{2}", scope.Key, coll.Key, dbConfig.Bucket);
                        await bucket.Collections.DropCollectionAsync(new CollectionSpec(scope.Key, coll.Key))
                            .ConfigureAwait(false);
                        Logger.LogInformation("\t...done");
                    }
                }
            }

            return Ok();
        }

        [HttpPost("{name}/Flush")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> FlushDatabase(string name)
        {
            var getDbResult = await TryAsyncReturn(_syncGatewayClient.GetDatabase, "getting database details", name)
                .ConfigureAwait(false);
            if(!getDbResult.Succeeded) {
                return getDbResult.Error!;
            }

            var cluster = await CouchbaseCluster.Instance().ConfigureAwait(false);

            foreach(var scope in getDbResult.Result!.Scopes) {
                foreach(var collection in scope.Value.Collections) {
                    try {
                        await cluster.QueryAsync<object>(
                            $"DELETE FROM {name}.{scope.Key}.{collection.Key} WHERE POSITION(meta().id, '_sync') != 0")
                            .ConfigureAwait(false);
                    } catch(Exception ex) {
                        Logger.LogError(ex, "Exception deleting from collection");
                        return new InternalServerErrorResult(new
                        {
                            message = ex.Message
                        });
                    }
                }
            }

            return Ok();
        }

        [HttpPost("{name}/BulkDocs")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> AddDocumentsToDatabase(string name, DatabaseAddDocuments addition)
        {
            if(!addition.IsValid) {
                return BadRequest(new
                {
                    message = "Invalid input to endpoint"
                });
            }

            var response = await TryAsyncReturn(_syncGatewayClient.BulkDocs, "putting bulk docs", 
                name, addition).ConfigureAwait(false);
            if(!response.Succeeded) {
                return response.Error!;
            }

            return Ok(response.Result);
        }

        private async Task CreatePrimaryIndex(ICluster cluster, string combinedName)
        {
            try {
                await cluster.QueryAsync<object>($"CREATE PRIMARY INDEX ON {combinedName}").ConfigureAwait(false);
            } catch(IndexExistsException) {
                Logger.LogTrace("Primary index already exists!");
            }
        }

        private async ValueTask<IBucket?> GetBucket(ICluster cluster, string bucketName)
        {
            try {
                Logger.LogInformation("Retrieving bucket {0}...", bucketName);
                var b = await cluster.BucketAsync(bucketName).ConfigureAwait(false);
                await b.WaitUntilReadyAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
                Logger.LogInformation("\t...done.");
                return b;
            } catch(Exception ex) {
                switch(ex) {
                    case AuthenticationFailureException:
                    case Couchbase.Management.Buckets.BucketNotFoundException:
                    case ObjectDisposedException:
                    case UnambiguousTimeoutException:
                        // This means the bucket was retrieved too soon after creation, so stall a bit.
                        Logger.LogWarning("Bucket not ready to be retrieved yet, waiting...");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        return await GetBucket(cluster, bucketName).ConfigureAwait(false);
                    default:
                        Logger.LogError(ex, "Exception while getting bucket");
                        return null;
                }
            }
        }
    }
}
