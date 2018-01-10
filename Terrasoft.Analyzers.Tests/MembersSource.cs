using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terrasoft.Analyzers.Tests
{

	#region Class: MembersSource

	public class MembersSource
	{
		public MembersSource() {
			
		}

		#region Constructors: ProtectedInternal

		protected internal MembersSource()
		{

		}

		#endregion

		#region Constructors: Private

		MembersSource()
		{

		}

		#endregion

		private delegate void MissRegionDelegate();

		#region Delegates: Internal

		delegate void DelegateInRegion();

		#endregion

		public const string MissRegionConst = "MissRegionConst";

		#region Constants: Private

		const string ConstInRegion = "ConstInRegion";

		#endregion

		public string MissRegionField = "MissRegionField";

		#region Fields: Private

		string FieldInRegion = "FieldInRegion";

		#endregion

		public string MissRegionProp { get; set; }

		#region Properties: Private

		string PropInRegion { get; set; }

		#endregion

		public event Action MissRegionEvent;

		#region Events: Internal

		event Action EventInRegion;

		#endregion

		public void MissRegionMethod() {
		}

		#region Methods: Private

		void MethodInRegion() {
		}

		#endregion

	}

	#endregion

}
