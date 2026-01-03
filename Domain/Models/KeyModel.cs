namespace Domain.Models
{
	public class KeyModel
	{
        public string Key { get; private set; }
        public double? TimeToLive { get; private set; }

        protected KeyModel(string key, double? timeToLive) 
		{
			Key = key;
            TimeToLive = timeToLive;
		}
		public static KeyModel Create(string key, double? timeToLive) 
		{
			return new KeyModel(key, timeToLive);
        }
    }
}
