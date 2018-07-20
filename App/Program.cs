﻿using System;
using System.Net;
using System.Threading;
using ETModel;
using Model.Fishs.Components;
using Model.Module.MySql;
using MongoDB.Bson.Serialization;
using NLog;
using ETHotfix;
using System.Threading.Tasks;

namespace App
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // 异步方法全部会回掉到主线程
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);

            try
            {
                Game.EventSystem.Add(DLLType.Model, typeof(Game).Assembly);
                //Game.EventSystem.Add(DLLType.Hotfix, DllHelper.GetHotfixAssembly());
                Game.EventSystem.Add(DLLType.Hotfix, typeof(Hotfix).Assembly);

                Options options = Game.Scene.AddComponent<OptionComponent, Options>(new Options { AppId = 1, AppType = AppType.Location, Config = "../../Config/StartConfig/LocalAllServer.json" }).Options;
                StartConfig startConfig = Game.Scene.AddComponent<StartConfigComponent, string, int>(options.Config, options.AppId).StartConfig;

                if (!options.AppType.Is(startConfig.AppType))
                {
                    Log.Error("命令行参数apptype与配置不一致");
                    return;
                }



                IdGenerater.AppId = options.AppId;

                LogManager.Configuration.Variables["appType"] = startConfig.AppType.ToString();
                LogManager.Configuration.Variables["appId"] = startConfig.AppId.ToString();
                LogManager.Configuration.Variables["appTypeFormat"] = $"{startConfig.AppType,-8}";
                LogManager.Configuration.Variables["appIdFormat"] = $"{startConfig.AppId:D3}";

                Log.Info($"server start........................ {startConfig.AppId} {startConfig.AppType}");

                Game.Scene.AddComponent<OpcodeTypeComponent>();
                Game.Scene.AddComponent<MessageDispatherComponent>();

                // 根据不同的AppType添加不同的组件
                OuterConfig outerConfig = startConfig.GetComponent<OuterConfig>();
                InnerConfig innerConfig = startConfig.GetComponent<InnerConfig>();
                ClientConfig clientConfig = startConfig.GetComponent<ClientConfig>();
                DBConfig dbConfig = startConfig.GetComponent<DBConfig>();
                switch (startConfig.AppType)
                {
                    case AppType.Manager:
                        Game.Scene.AddComponent<NetInnerComponent, IPEndPoint>(innerConfig.IPEndPoint);
                        Game.Scene.AddComponent<NetOuterComponent, IPEndPoint>(outerConfig.IPEndPoint);
                        Game.Scene.AddComponent<AppManagerComponent>();
                        break;
                    case AppType.Realm:
                        Game.Scene.AddComponent<ActorMessageDispatherComponent>();
                        Game.Scene.AddComponent<NetInnerComponent, IPEndPoint>(innerConfig.IPEndPoint);
                        Game.Scene.AddComponent<NetOuterComponent, IPEndPoint>(outerConfig.IPEndPoint);
                        Game.Scene.AddComponent<LocationProxyComponent>();
                        Game.Scene.AddComponent<RealmGateAddressComponent>();
                        break;
                    case AppType.Gate:         
                        Game.Scene.AddComponent<ActorMessageDispatherComponent>();
                        Game.Scene.AddComponent<NetInnerComponent, IPEndPoint>(innerConfig.IPEndPoint);
                        Game.Scene.AddComponent<NetOuterComponent, IPEndPoint>(outerConfig.IPEndPoint);
                        Game.Scene.AddComponent<LocationProxyComponent>();
                        Game.Scene.AddComponent<ActorMessageSenderComponent>();
                        Game.Scene.AddComponent<GateSessionKeyComponent>();
                        break;
                    case AppType.Location:
                        Game.Scene.AddComponent<NetInnerComponent, IPEndPoint>(innerConfig.IPEndPoint);
                        Game.Scene.AddComponent<LocationComponent>();
                        break;
                    case AppType.Map:
                        Game.Scene.AddComponent<NetInnerComponent, IPEndPoint>(innerConfig.IPEndPoint);
                        Game.Scene.AddComponent<UnitComponent>();
                        Game.Scene.AddComponent<LocationProxyComponent>();
                        Game.Scene.AddComponent<ActorMessageSenderComponent>();
                        Game.Scene.AddComponent<ActorMessageDispatherComponent>();
                        Game.Scene.AddComponent<ServerFrameComponent>();
                        break;
                    case AppType.AllServer:
                        Game.Scene.AddComponent<ActorMessageSenderComponent>();
                        //Game.Scene.AddComponent<PlayerComponent>();
                        Game.Scene.AddComponent<UnitComponent>();
                        Game.Scene.AddComponent<DBComponent>();
                        Game.Scene.AddComponent<DBProxyComponent>();
                        Game.Scene.AddComponent<DBCacheComponent>();
                        Game.Scene.AddComponent<LocationComponent>();
                        Game.Scene.AddComponent<ActorMessageDispatherComponent>();
                        Game.Scene.AddComponent<NetInnerComponent, IPEndPoint>(innerConfig.IPEndPoint);
                        Game.Scene.AddComponent<NetOuterComponent, IPEndPoint>(outerConfig.IPEndPoint);
                        Game.Scene.AddComponent<LocationProxyComponent>();
                        Game.Scene.AddComponent<AppManagerComponent>();
                        Game.Scene.AddComponent<RealmGateAddressComponent>();
                        Game.Scene.AddComponent<GateSessionKeyComponent>();
                        Game.Scene.AddComponent<ConfigComponent>();
                        Game.Scene.AddComponent<ServerFrameComponent>();
                        Game.Scene.AddComponent<UnitManageComponent>();
                        Game.Scene.AddComponent<SqlComponent, string>(dbConfig.ConnectionString);
                        // Game.Scene.AddComponent<HttpComponent>();
                        break;
                    case AppType.Benchmark:
                        Game.Scene.AddComponent<NetOuterComponent>();
                        Game.Scene.AddComponent<BenchmarkComponent, IPEndPoint>(clientConfig.IPEndPoint);
                        break;
                    default:
                        throw new Exception($"命令行参数没有设置正确的AppType: {startConfig.AppType}");
                }
                var locationProxyComponent = Game.Scene.GetComponent<LocationProxyComponent>();
                if (locationProxyComponent != null && startConfig.AppType != AppType.AllServer) //需要注册
                {
                    var a = locationProxyComponent.RegisterServer().Result;
                    Log.Debug(">>>" + a);

                }



                while (true)
                {
                    try
                    {
                        Thread.Sleep(1);
                        OneThreadSynchronizationContext.Instance.Update();
                        Game.EventSystem.Update();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
