using System;


namespace ETModel
{

	public abstract class ComponentWithId : Component
	{
		public long Id { get; set; }

		protected ComponentWithId()
		{
			this.Id = this.InstanceId;
		}

		protected ComponentWithId(long id)
		{
			this.Id = id;
		}

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			base.Dispose();
		}
	}
}