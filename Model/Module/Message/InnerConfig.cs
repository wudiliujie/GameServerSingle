using System.Net;
namespace ETModel
{

	public class InnerConfig: AConfigComponent
	{
		public string Host { get; set; }
		public int Port { get; set; }

		private IPEndPoint ipEndPoint;

		public override void EndInit()
		{
			base.EndInit();

			this.ipEndPoint = NetworkHelper.ToIPEndPoint(this.Host, this.Port);
		}


		public IPEndPoint IPEndPoint
		{
			get
			{
				return this.ipEndPoint;
			}
		}
	}
}