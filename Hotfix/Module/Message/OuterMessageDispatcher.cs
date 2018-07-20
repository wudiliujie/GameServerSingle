using System;
using ETModel;
using Google.Protobuf;
using Model.Fishs.Components;

namespace ETHotfix
{
    public class OuterMessageDispatcher : IMessageDispatcher
    {
        public  void Dispatch(Session session, Packet packet)
        {
            Log.Debug("分配消息" + packet.Opcode);
            IMessage message;
            try
            {
                message = session.Network.Entity.GetComponent<OpcodeTypeComponent>().GetNewMessage(packet.Opcode);
                message.MergeFrom(packet.Bytes, packet.Offset, packet.Length);
                //message = session.Network.MessagePacker.DeserializeFrom(messageType, packet.Bytes, Packet.Index, packet.Length - Packet.Index);

            }
            catch (Exception e)
            {
                // 出现任何异常都要断开Session，防止客户端伪造消息
                Log.Error(e);
                session.Error = ErrorCode.ERR_PacketParserError;
                session.Network.Remove(session.Id);
                return;
            }

            //Log.Debug($"recv: {JsonHelper.ToJson(message)}");

            switch (message)
            {
                case IFrameMessage iFrameMessage: // 如果是帧消息，构造成OneFrameMessage发给对应的unit
                    {
                        long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                        ActorMessageSender actorMessageSender = Game.Scene.GetComponent<ActorMessageSenderComponent>().Get(unitId);
                        // 这里设置了帧消息的id，防止客户端伪造
                        iFrameMessage.Id = unitId;
                        return;
                    }
                case IActorMessage iActorMessage: // gate session收到actor消息直接转发给actor自己去处理
                    {
                        session.GetComponent<SessionPlayerComponent>().Player.GetComponent<MailBoxComponent>().Add(new ActorMessageInfo() { Session = session, Message = iActorMessage });
                        return;
                    }
            }

            Game.Scene.GetComponent<MessageDispatherComponent>().Handle(session, new MessageInfo(packet.Opcode, message));
        }
    }
}
