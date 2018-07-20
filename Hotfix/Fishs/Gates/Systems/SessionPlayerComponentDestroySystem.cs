﻿using ETHotfix.Fishs.Maps.Systems;
using ETModel;
using Model.Fishs.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace ETHotfix.Fishs.Gates.Systems
{
    [ObjectSystem]
    public class SessionPlayerComponentDestroySystem : DestroySystem<SessionPlayerComponent>
    {
        public override  void Destroy(SessionPlayerComponent self)
        {
            // 发送断线消息
            //ActorMessageSender actorMessageSender = Game.Scene.GetComponent<ActorMessageSenderComponent>().Get(self.Player.UnitId);
            //actorMessageSender.Send(new G2M_SessionDisconnect());
            Log.Debug("断开连接:"+self.Player.Id);

            var unitRoom = self.Player.GetComponent<UnitRoomComponent>();
            if (unitRoom != null)
            {
                Log.Debug("离开room");
                unitRoom.LeaveRoom();
            }
            self.Player.Dispose();
            Game.Scene.GetComponent<PlayerManagerComponent>()?.Remove(self.Player.Id);
        }
    }
}
