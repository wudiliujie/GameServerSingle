﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace ETModel
{
    public abstract class NetworkComponent : Component
    {
        public AppType AppType;

        private AService Service;

        private readonly Dictionary<long, Session> sessions = new Dictionary<long, Session>();

        public IMessagePacker MessagePacker { get; set; }

        public IMessageDispatcher MessageDispatcher { get; set; }

        public void Awake(NetworkProtocol protocol)
        {
            try
            {
                switch (protocol)
                {
                    case NetworkProtocol.KCP:
                        this.Service = new KService();
                        break;
                    case NetworkProtocol.TCP:
                        this.Service = new TService();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                this.Service.AcceptCallback += this.OnAccept;
            }
            catch (Exception e)
            {
                throw new Exception($"{e}");
            }
        }

        public void Awake(NetworkProtocol protocol, IPEndPoint ipEndPoint)
        {
            try
            {
                switch (protocol)
                {
                    case NetworkProtocol.KCP:
                        this.Service = new KService(ipEndPoint);
                        break;
                    case NetworkProtocol.TCP:
                        this.Service = new TService(ipEndPoint);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                this.Service.AcceptCallback += this.OnAccept;
            }
            catch (Exception e)
            {
                throw new Exception($"{ipEndPoint}", e);
            }
        }

        public void Start()
        {
            this.Service.Start();
        }

        public void OnAccept(AChannel channel)
        {
            Console.WriteLine("OnAccept:"+Thread.CurrentThread.ManagedThreadId);
            Session session = ComponentFactory.CreateWithParent<Session, NetworkComponent, AChannel>(this, this, channel);
            this.sessions.Add(session.Id, session);
        }

        public virtual void Remove(long id)
        {
            Session session;
            if (!this.sessions.TryGetValue(id, out session))
            {
                return;
            }
            this.sessions.Remove(id);
            session.Dispose();
        }

        public Session Get(long id)
        {
            Session session;
            this.sessions.TryGetValue(id, out session);
            return session;
        }

        /// <summary>
        /// 创建一个新Session
        /// </summary>
        public Session Create(IPEndPoint ipEndPoint)
        {
            AChannel channel = this.Service.ConnectChannel(ipEndPoint);
            Session session = ComponentFactory.CreateWithParent<Session, NetworkComponent, AChannel>(this, this, channel);
            this.sessions.Add(session.Id, session);
            return session;
        }

        public void Update()
        {
            if (this.Service == null)
            {
                return;
            }
            this.Service.Update();
        }

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            base.Dispose();

            foreach (Session session in this.sessions.Values.ToArray())
            {
                session.Dispose();
            }

            this.Service.Dispose();
        }
    }
}