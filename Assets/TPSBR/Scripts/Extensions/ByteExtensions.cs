namespace TPSBR
{
	public static partial class ByteExtensions
	{
		// PUBLIC METHODS

		public static bool IsBitSet(this byte flags, int bit)
		{
			return (flags & (1 << bit)) == (1 << bit);
		}

		public static byte SetBit(ref this byte flags, int bit, bool value)
		{
			if (value == true)
			{
				return flags |= (byte)(1 << bit);
			}
			else
			{
				return flags &= unchecked((byte)~(1 << bit));
			}
		}

		public static byte SetBitNoRef(this byte flags, int bit, bool value)
		{
			if (value == true)
			{
				return flags |= (byte)(1 << bit);
			}
			else
			{
				return flags &= unchecked((byte)~(1 << bit));
			}
		}
	}
}
