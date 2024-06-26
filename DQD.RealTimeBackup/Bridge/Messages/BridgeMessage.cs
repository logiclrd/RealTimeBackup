using System;
using System.Collections.Generic;
using System.Linq;

using DQD.RealTimeBackup.Bridge.Serialization;
using DQD.RealTimeBackup.Utility;

namespace DQD.RealTimeBackup.Bridge.Messages
{
	public abstract class BridgeMessage
	{
		public abstract BridgeMessageType MessageType { get; }

		public void SerializeWithLengthPrefix(ByteBuffer buffer)
		{
			int lengthOffset = buffer.Length;

			buffer.AppendInt32(0); // Dummy value, to be replaced

			int lengthBefore = buffer.Length;

			buffer.AppendInt32((int)MessageType);

			ByteBufferSerializer.Instance.SerializeNotNull(GetType(), this, buffer);

			int lengthAfter = buffer.Length;

			int messageLength = lengthAfter - lengthBefore;

			byte[] messageLengthBytes = BitConverter.GetBytes(messageLength);

			for (int i=0; i < 4; i++)
				buffer[lengthOffset + i] = messageLengthBytes[i];
		}

		public void Serialize(ByteBuffer buffer)
		{
			buffer.AppendInt32((int)MessageType);

			ByteBufferSerializer.Instance.Serialize(GetType(), this, buffer);
		}

		static Dictionary<int, Type> s_bridgeMessageTypes =
			typeof(BridgeMessage).Assembly.GetTypes()
			.Where(type => !type.IsAbstract && typeof(BridgeMessage).IsAssignableFrom(type))
			.Select(type => (BridgeMessage)Activator.CreateInstance(type)!)
			.ToDictionary(
				keySelector: instance => (int)instance.MessageType,
				elementSelector: instance => instance.GetType());

		public static bool DeserializeWithLengthPrefix<TMessageBase>(ByteBuffer buffer, out TMessageBase message)
			where TMessageBase : BridgeMessage
		{
			message = default!;

			if (!buffer.TryPeekInt32(out var messageLength))
				return false;
			if (buffer.Length - 4 < messageLength)
				return false;

			buffer.Consume(4);

			int messageType = buffer.ReadInt32();

			messageLength -= 4;

			if (!s_bridgeMessageTypes.TryGetValue(messageType, out var type)
			 || (type == null)
			 || !typeof(TMessageBase).IsAssignableFrom(type))
			{
				buffer.Consume(messageLength - 4);
				throw new Exception("Unrecognized message type: " + messageType);
			}
			else
			{
				int lengthBefore = buffer.Length;

				message = (TMessageBase)ByteBufferSerializer.Instance.DeserializeNotNull(type, buffer)!;

				int lengthAfter = buffer.Length;

				int bytesConsumed = lengthBefore - lengthAfter;

				if (bytesConsumed > messageLength)
					throw new Exception($"Protocol error: {type.Name}.Deserialize consumed {bytesConsumed} bytes, but the message length prefix was only {messageLength}");

				if (bytesConsumed < messageLength)
					buffer.Consume(messageLength - bytesConsumed);

				return true;
			}
		}
	}
}
