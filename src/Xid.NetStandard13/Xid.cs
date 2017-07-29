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
			//Not using the null coalescing operator as it doesn't treat "" the same as null.
			var retVal = Environment.GetEnvironmentVariable("COMPUTERNAME");
			if (String.IsNullOrEmpty(retVal))
				retVal = Environment.GetEnvironmentVariable("HOSTNAME");
			if (String.IsNullOrEmpty(retVal))
				retVal = System.Net.Dns.GetHostName();

			return retVal;
		}
	}
}