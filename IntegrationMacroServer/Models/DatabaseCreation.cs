namespace IntegrationMacroServer.Models
{
    public class DatabaseCreation : IValidatable
    {
        public string? Name { get; set; }
        
        public string? Bucket { get; set; }

        public IReadOnlyList<string>? Collections { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool IsValid
        {
            get { 
                if(Name == null || Bucket == null) {
                    return false;
                }
                
                return (Username == null) == (Password == null);
            }
        }
    }

    public class DatabaseCreationResult
    {
        public string Url { get; }

        public DatabaseCreationResult(string url)
        {
            Url = url;
        }
    }
}