using System;


namespace ETModel
{
	public class StartConfig: Entity
	{
		public int AppId { get; set; }

		public AppType AppType { get; set; }

		public string ServerIP { get; set; }
    }
}