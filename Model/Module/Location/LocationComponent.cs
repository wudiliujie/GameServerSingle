﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ETModel
{
	public abstract class LocationTask: Component
	{
		public abstract void Run();
	}
	
	[ObjectSystem]
	public class LocationQueryTaskAwakeSystem : AwakeSystem<LocationQueryTask, long>
	{
		public override void Awake(LocationQueryTask self, long key)
		{
			self.Key = key;
			self.Tcs = new TaskCompletionSource<ObjectInfo>();
		}
	}

	public sealed class LocationQueryTask : LocationTask
	{
		public long Key;

		public TaskCompletionSource<ObjectInfo> Tcs;

		public Task<ObjectInfo> Task
		{
			get
			{
				return this.Tcs.Task;
			}
		}

		public override void Run()
		{
			try
			{
				LocationComponent locationComponent = this.GetParent<LocationComponent>();
				ObjectInfo location = locationComponent.Get(this.Key);
				this.Tcs.SetResult(location);
			}
			catch (Exception e)
			{
				this.Tcs.SetException(e);
			}
		}
	}

	public class LocationComponent : Component
	{
		private readonly Dictionary<long, ObjectInfo> locations = new Dictionary<long, ObjectInfo>();

		private readonly Dictionary<long, long> lockDict = new Dictionary<long, long>();

		private readonly Dictionary<long, Queue<LocationTask>> taskQueues = new Dictionary<long, Queue<LocationTask>>();

		public void Add(ObjectInfo info)
		{
            this.locations[info.Key] = info;

			Log.Info($"location add key: {info.Key} instanceId: {info.InstanceId}");

			// 更新db
			//await Game.Scene.GetComponent<DBProxyComponent>().Save(new Location(key, address));
		}

		public void Remove(long key)
		{
			Log.Info($"location remove key: {key}");
			this.locations.Remove(key);
		}

		public ObjectInfo Get(long key)
		{
			this.locations.TryGetValue(key, out ObjectInfo instanceId);
			return instanceId;
		}

		public async void Lock(long key, long instanceId, int time = 0)
		{
			if (this.lockDict.ContainsKey(key))
			{
				Log.Error($"不可能同时存在两次lock, key: {key} InstanceId: {instanceId}");
				return;
			}

			Log.Info($"location lock key: {key} InstanceId: {instanceId}");

			if (!this.locations.TryGetValue(key, out ObjectInfo saveInstanceId))
			{
				Log.Error($"actor没有注册, key: {key} InstanceId: {instanceId}");
				return;
			}

			if (saveInstanceId.InstanceId != instanceId)
			{
				Log.Error($"actor注册的instanceId与lock的不一致, key: {key} InstanceId: {instanceId} saveInstanceId: {saveInstanceId}");
				return;
			}

			this.lockDict.Add(key, instanceId);

			// 超时则解锁
			if (time > 0)
			{
				await Game.Scene.GetComponent<TimerComponent>().WaitAsync(time);

				if (!this.lockDict.ContainsKey(key))
				{
					return;
				}
				Log.Info($"location timeout unlock key: {key} time: {time}");
				this.UnLock(key);
			}
		}

		public void UnLockAndUpdate(long key, long oldInstanceId, long instanceId)
		{
			this.lockDict.TryGetValue(key, out long lockInstanceId);
			if (lockInstanceId != oldInstanceId)
			{
				Log.Error($"unlock appid is different {lockInstanceId} {oldInstanceId}" );
			}
			Log.Info($"location unlock key: {key} oldInstanceId: {oldInstanceId} new: {instanceId}");
			this.locations[key].InstanceId = instanceId;
			this.UnLock(key);
		}

		private void UnLock(long key)
		{
			this.lockDict.Remove(key);

			if (!this.taskQueues.TryGetValue(key, out Queue<LocationTask> tasks))
			{
				return;
			}

			while (true)
			{
				if (tasks.Count <= 0)
				{
					this.taskQueues.Remove(key);
					return;
				}
				if (this.lockDict.ContainsKey(key))
				{
					return;
				}

				LocationTask task = tasks.Dequeue();
				try
				{
					task.Run();
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
				task.Dispose();
			}
		}

		public Task<ObjectInfo> GetAsync(long key)
		{
			if (!this.lockDict.ContainsKey(key))
			{
				this.locations.TryGetValue(key, out ObjectInfo instanceId);
				Log.Info($"location get key: {key} {instanceId}");
				return Task.FromResult(instanceId);
			}

			LocationQueryTask task = ComponentFactory.CreateWithParent<LocationQueryTask, long>(this, key);
			this.AddTask(key, task);
			return task.Task;
		}

		public void AddTask(long key, LocationTask task)
		{
			if (!this.taskQueues.TryGetValue(key, out Queue<LocationTask> tasks))
			{
				tasks = new Queue<LocationTask>();
				this.taskQueues[key] = tasks;
			}
			tasks.Enqueue(task);
		}

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			base.Dispose();
			
			this.locations.Clear();
			this.lockDict.Clear();
			this.taskQueues.Clear();
		}
	}
}