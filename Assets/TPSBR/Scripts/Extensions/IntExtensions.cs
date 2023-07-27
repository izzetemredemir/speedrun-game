namespace TPSBR
{
	public static partial class ShortExtensions
	{
		// PUBLIC METHODS

		public static bool IsBitSet(this int flags, int bit)
		{
			return (flags & (1 << bit)) == (1 << bit);
		}

		public static int SetBit(ref this int flags, int bit, bool value)
		{
			if (value == true)
			{
				return flags |= (1 << bit);
			}
			else
			{
				return flags &= unchecked(~(1 << bit));
			}
		}
	}
}
