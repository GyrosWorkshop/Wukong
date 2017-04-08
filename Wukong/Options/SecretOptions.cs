namespace Wukong.Options
{
    public class SecretOptions
    {
        public Secret Google { get; set; }
        public Secret GitHub { get; set; }
        public Secret Microsoft { get; set; }
    }

    public class Secret
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

}