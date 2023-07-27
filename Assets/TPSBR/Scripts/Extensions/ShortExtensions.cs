namespace TPSBR
{
	public static partial class ShortExtensions
	{
		// PUBLIC METHODS

		public static bool IsBitSet(this short flags, int bit)
		{
			return (flags & (1 << bit)) == (1 << bit);
		}

		public static short SetBit(ref this short flags, int bit, bool value)
		{
			if (value == true)
			{
				return flags |= (short)(1 << bit);
			}
			else
			{
				return flags &= unchecked((short)~(1 << bit));
			}
		}
	}
}
