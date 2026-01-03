namespace Domain.Models
{
	public class KeyValueModel : KeyModel
    {
        public string? Value { get; private set; }

        private KeyValueModel(string key, string? value, double? timeToLive) : base(key, timeToLive)
        {
			Value = value;
		}

        public static KeyValueModel Create(string key, string? value, double? timeToLive)
        {
            return new KeyValueModel(key, value, timeToLive);
        }
    }
}
