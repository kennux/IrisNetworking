using UnityEngine;
using System.Collections;

public class BitUtils
{
	public static void Set(ref byte aByte, int pos, bool value)
	{
		if (value)
		{
			//left-shift 1, then bitwise OR
			aByte = (byte)(aByte | (1 << pos));
		}
		else
		{
			//left-shift 1, then take complement, then bitwise AND
			aByte = (byte)(aByte & ~(1 << pos));
		}
	}
	
	public static bool Get(byte aByte, int pos)
	{
		//left-shift 1, then bitwise AND, then check for non-zero
		return ((aByte & (1 << pos)) != 0);
	}
}
