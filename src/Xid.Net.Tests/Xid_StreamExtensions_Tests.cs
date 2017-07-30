using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XidNet.Tests
{
	[TestClass]
	public class Xid_StreamExtensions_Tests
	{

		[TestMethod]
		public void Stream_WriteXid_CorrectlyWritesXid()
		{
			using (var ms = new System.IO.MemoryStream(Xid.Length))
			{
				var x = Xid.NewXid();
				ms.WriteXid(x);

				Assert.AreEqual(Xid.Length, ms.Length);
				Assert.AreEqual(Xid.Length, ms.Position);

				ms.Seek(0, System.IO.SeekOrigin.Begin);
				var b = new byte[Xid.Length];
				ms.Read(b, 0, b.Length);

				var y = new Xid(b);
				Assert.AreEqual(x, y);
			}
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void Stream_WriteXid_ThrowsOnNullStream()
		{
			System.IO.MemoryStream ms = null; 
			var x = Xid.NewXid();
			ms.WriteXid(x);
		}



		[TestMethod]
		public void Stream_ReadXid_CorrectlyReadsXid()
		{
			using (var ms = new System.IO.MemoryStream(Xid.Length))
			{
				var x = Xid.NewXid();
				ms.WriteXid(x);

				Assert.AreEqual(Xid.Length, ms.Length);
				Assert.AreEqual(Xid.Length, ms.Position);

				ms.Seek(0, System.IO.SeekOrigin.Begin);
				var y = ms.ReadXid();

				Assert.AreEqual(x, y);
				Assert.AreEqual(Xid.Length, ms.Length);
				Assert.AreEqual(Xid.Length, ms.Position);
			}
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void Stream_ReadXid_ThrowsOnNullStream()
		{
			System.IO.MemoryStream ms = null;
			ms.ReadXid();
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void Stream_ReadXid_ThrowsWhenInsufficientBytesRemaining()
		{
			using (var ms = new System.IO.MemoryStream(Xid.Length))
			{
				var x = Xid.NewXid();
				ms.WriteXid(x);

				Assert.AreEqual(Xid.Length, ms.Length);
				Assert.AreEqual(Xid.Length, ms.Position);

				ms.Seek(6, System.IO.SeekOrigin.Begin);
				var y = ms.ReadXid();
			}
		}

	}
}