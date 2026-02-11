using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class TcpClientExample : MonoBehaviour
{
	private TcpClient client;
	private NetworkStream stream;
	private Thread receiveThread;
	private byte[] buffer = new byte[1024 * 4];

	// 수신 버퍼
	private List<byte> receiveBuffer = new List<byte>();

	public void Connect(string ip, int port)
	{
		string IP = "52.78.148.28";
		int PORT = 8000;

		client = new TcpClient();
		client.Connect(IPAddress.Parse(IP), PORT);
		stream = client.GetStream();

		receiveThread = new Thread(ReceiveLoop);
		receiveThread.IsBackground = true;
		receiveThread.Start();
	}

	private void ReceiveLoop()
	{
		while (true)
		{
			try
			{
				int byteCount = stream.Read(buffer, 0, buffer.Length);
				if (byteCount <= 0)
				{
					Debug.LogWarning("서버 연결 끊김");
					break;
				}

				// 받은 데이터 버퍼에 저장
				receiveBuffer.AddRange(buffer[..byteCount]);

				// 패킷 파싱 시도
				ParsePackets();
			}
			catch (Exception e)
			{
				Debug.LogWarning($"예외: {e.Message}");
				break;
			}
		}
	}

	private void ParsePackets()
	{
		while (true)
		{
			// 최소한 4바이트는 있어야 길이 읽을 수 있음
			if (receiveBuffer.Count < 4)
				return;

			// 길이 읽기
			int packetLength = BitConverter.ToInt32(receiveBuffer.ToArray(), 0);

			// 전체 패킷이 도착했는지 확인
			if (receiveBuffer.Count < 4 + packetLength)
				return;

			// 패킷 추출
			byte[] packetData = receiveBuffer.GetRange(4, packetLength).ToArray();

			// 버퍼에서 제거
			receiveBuffer.RemoveRange(0, 4 + packetLength);

			// 받은 데이터 처리 (예: 문자열로 출력)
			string message = Encoding.UTF8.GetString(packetData);
			
			Debug.LogWarning($"받은 메시지: {message}");
		}
	}

	public void Send(string message)
	{
		if (stream == null) return;

		byte[] data = Encoding.UTF8.GetBytes(message);
		byte[] length = BitConverter.GetBytes(data.Length);

		byte[] packet = new byte[length.Length + data.Length];
		Buffer.BlockCopy(length, 0, packet, 0, length.Length);
		Buffer.BlockCopy(data, 0, packet, length.Length, data.Length);

		stream.Write(packet, 0, packet.Length);
	}
}
