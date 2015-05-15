using UnityEngine;
using System.Collections;
using IrisNetworking;

public static class IrisExtension
{
	// SERIALIZATION FOR UNITY TYPES
	public static void Serialize(this IrisStream stream, ref Vector3 vector)
	{
		stream.Serialize (ref vector.x);
		stream.Serialize (ref vector.y);
		stream.Serialize (ref vector.z);
	}
	public static void Serialize(this IrisStream stream, ref Quaternion quaternion)
	{
		stream.Serialize (ref quaternion.x);
		stream.Serialize (ref quaternion.y);
		stream.Serialize (ref quaternion.z);
		stream.Serialize (ref quaternion.w);
	}
}
