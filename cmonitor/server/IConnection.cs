﻿using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace cmonitor.server
{
    /// <summary>
    /// 连接对象
    /// </summary>
    public interface IConnection
    {
        public string Name { get; set; }
        /// <summary>
        /// <summary>
        /// 已连接
        /// </summary>
        public bool Connected { get; }

        public IPEndPoint Address { get; }
        public IPEndPoint LocalAddress { get; }

        public SslStream TcpSourceSocket { get; }
        public SslStream TcpTargetSocket { get; set; }

        #region 接收数据
        /// <summary>
        /// 请求数据包装对象
        /// </summary>
        public MessageRequestWrap ReceiveRequestWrap { get; }
        /// <summary>
        /// 回复数据包装对象
        /// </summary>
        public MessageResponseWrap ReceiveResponseWrap { get; }
        /// <summary>
        /// 接收到的原始数据
        /// </summary>
        public ReadOnlyMemory<byte> ReceiveData { get; set; }
        #endregion

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<bool> Send(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Task<bool> Send(byte[] data, int length);

        /// <summary>
        /// 销毁
        /// </summary>
        public void Disponse();

        #region 回复消息相关

        public Memory<byte> ResponseData { get; }
        public void Write(Memory<byte> data);
        public void Write(ulong num);
        public void Write(ushort num);
        public void Write(ushort[] nums);
        /// <summary>
        /// 英文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF8(string str);
        /// <summary>
        /// 中文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF16(string str);
        /// <summary>
        /// 归还池
        /// </summary>
        public void Return();
        #endregion

    }

    public abstract class Connection : IConnection
    {
        public Connection()
        {
        }

        public string Name { get; set; }
        /// <summary>
        /// 已连接
        /// </summary>
        public virtual bool Connected => false;
        /// <summary>
        /// 地址
        /// </summary>
        public IPEndPoint Address { get; protected set; }
        public IPEndPoint LocalAddress { get; protected set; }

        public SslStream TcpSourceSocket { get; protected set; }
        public SslStream TcpTargetSocket { get; set; }
        public bool Relayed { get; set; }

        #region 接收数据
        /// <summary>
        /// 接收请求数据
        /// </summary>
        public MessageRequestWrap ReceiveRequestWrap { get; set; }
        /// <summary>
        /// 接收回执数据
        /// </summary>
        public MessageResponseWrap ReceiveResponseWrap { get; set; }
        /// <summary>
        /// 接收数据
        /// </summary>
        public ReadOnlyMemory<byte> ReceiveData { get; set; }
        #endregion

        #region 回复数据
        public Memory<byte> ResponseData { get; private set; }
        private byte[] responseData;
        private int length = 0;

        public void Write(Memory<byte> data)
        {
            ResponseData = data;
        }
        public void Write(ulong num)
        {
            length = 8;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            num.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        public void Write(ushort num)
        {
            length = 2;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            num.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        public void Write(ushort[] nums)
        {
            length = nums.Length * 2;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            nums.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        /// <summary>
        /// 英文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF8(string str)
        {
            var span = str.AsSpan();
            responseData = ArrayPool<byte>.Shared.Rent((span.Length + 1) * 3 + 8);
            var memory = responseData.AsMemory();

            int utf8Length = span.ToUTF8Bytes(memory.Slice(8));
            span.Length.ToBytes(memory);
            utf8Length.ToBytes(memory.Slice(4));
            length = utf8Length + 8;

            ResponseData = responseData.AsMemory(0, length);
        }
        /// <summary>
        /// 中文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF16(string str)
        {
            var span = str.GetUTF16Bytes();
            length = span.Length + 4;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            str.Length.ToBytes(responseData);
            span.CopyTo(responseData.AsSpan(4));

            ResponseData = responseData.AsMemory(0, length);
        }
        /// <summary>
        /// 归还池
        /// </summary>
        public void Return()
        {
            if (length > 0 && ResponseData.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(responseData);
            }
            ResponseData = Helper.EmptyArray;
            responseData = null;
            length = 0;
        }
        #endregion

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract Task<bool> Send(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract Task<bool> Send(byte[] data, int length);

        /// <summary>
        /// 销毁
        /// </summary>
        public virtual void Disponse()
        {
        }


    }


    public sealed class TcpConnection : Connection
    {
        public TcpConnection(SslStream stream,IPEndPoint local, IPEndPoint remote) : base()
        {
            TcpSourceSocket = stream;

            if (remote.Address.AddressFamily == AddressFamily.InterNetworkV6 && remote.Address.IsIPv4MappedToIPv6)
            {
                remote = new IPEndPoint(new IPAddress(remote.Address.GetAddressBytes()[^4..]), remote.Port);
            }
            Address = remote;

            if (local.Address.AddressFamily == AddressFamily.InterNetworkV6 && local.Address.IsIPv4MappedToIPv6)
            {
                local = new IPEndPoint(new IPAddress(local.Address.GetAddressBytes()[^4..]), local.Port);
            }
            LocalAddress = local;
        }

        /// <summary>
        /// 已连接
        /// </summary>
        public override bool Connected => TcpSourceSocket != null && TcpSourceSocket.CanWrite;

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override async Task<bool> Send(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
                    await TcpSourceSocket.WriteAsync(data);
                    await TcpSourceSocket.FlushAsync();
                    //SentBytes += (ulong)data.Length;
                    return true;
                }
                catch (Exception ex)
                {
                    Disponse();
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error(ex);
                }
            }
            return false;
        }
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override async Task<bool> Send(byte[] data, int length)
        {
            return await Send(data.AsMemory(0, length));
        }
        /// <summary>
        /// 销毁
        /// </summary>
        public override void Disponse()
        {
            base.Disponse();
            try
            {
                if (TcpSourceSocket != null)
                {
                    TcpSourceSocket.ShutdownAsync();
                    TcpSourceSocket.Dispose();

                    TcpTargetSocket?.ShutdownAsync();
                    TcpTargetSocket?.Dispose();
                }
            }
            catch (Exception)
            {
            }
        }
    }

}
