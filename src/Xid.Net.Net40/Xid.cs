using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XidNet
{
	public partial struct Xid
	{
		private static string ReadMachineName()
		{
			return Environment.MachineName;
		}
	}
}