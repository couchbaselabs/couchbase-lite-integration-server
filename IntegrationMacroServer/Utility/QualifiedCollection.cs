using Couchbase;
using Couchbase.KeyValue;
using Couchbase.Management.Collections;
using Serilog;

namespace IntegrationMacroServer.Utility
{
    public class QualifiedCollection
    {
        private const string DefaultName = "_default";

        public string Scope { get; }

        public string Collection { get; }

        public QualifiedCollection(string rawName)
        {
            if(String.IsNullOrEmpty(rawName)) {
                Scope = DefaultName;
                Collection = DefaultName;
                return;
            }

            var parts = rawName.Split(".");
            if(parts.Length == 1) {
                Scope = DefaultName;
                Collection = parts[0];
            } else {
                Scope = parts[0];
                Collection = parts[1];
            }
        }

        public async Task CreateIn(IBucket bucket)
        {
            if(Scope == DefaultName) {
                var scope = bucket.DefaultScope();
                if(scope == null) {
                    throw new ApplicationException("Default scope not found...");
                }
            } else {
                try {
                    await bucket.Collections.CreateScopeAsync(Scope).ConfigureAwait(false);
                } catch(ScopeExistsException) {

                } catch(ArgumentNullException) {
                    // https://issues.couchbase.com/browse/NCBC-3278
                    // This seems to happen in docker for whatever reason
                    // possibly timing related
                    Log.Logger.Warning("ArgumentNullException when trying to create scope, retrying...");
                    await Task.Delay(200).ConfigureAwait(false);
                    await CreateIn(bucket);
                }
            }

            try {
                await bucket.Collections.CreateCollectionAsync(new CollectionSpec(Scope, Collection));
            } catch (CollectionExistsException) {

            }
        }
    }
}
