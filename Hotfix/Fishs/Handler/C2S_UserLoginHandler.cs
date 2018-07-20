using ETModel;
using Model.Module.MySql;
using System;
using System.Collections.Generic;
using System.Text;
using ETHotfix.Module.Sql;
using Model.Fishs.Components;
using ETHotfix.Fishs.Systems;
using System.Net;
using Model.Fishs.Entitys;

namespace ETHotfix
{
    [MessageHandler(AppType.Gate)]
    public class C2S_UserLoginHandler : AMRpcHandler<C2S_UserLogin, S2C_UserLogin>
    {        
        protected async override void Run(Session session, C2S_UserLogin message, Action<S2C_UserLogin> reply)
        {
            S2C_UserLogin response = new S2C_UserLogin();
            //直接创建一个unit
            Model.Fishs.Entitys.Unit unit = ComponentFactory.Create<Model.Fishs.Entitys.Unit, UnitType>(UnitType.Hero);
            unit.AddComponent<MailBoxComponent>();
            unit.AddComponent<UnitGateComponent, long>(session.Id);
            unit.AddComponent<PlayerDbComponent, int>(message.AccountId);
            var initRet = await unit.GetComponent<PlayerDbComponent>().InitDataSync();
            if (initRet)
            {
                Log.Debug("unitId:" + unit.Id);
                Game.Scene.GetComponent<UnitManageComponent>().Add(unit);
            }
            response.Tag = 0;
            Log.Debug("数据库结束:" + unit.Id);
            session.AddComponent<SessionPlayerComponent>().Player = unit;
            response.UnitId = unit.Id;
            reply(response);
        }
    }
}
