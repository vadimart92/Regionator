﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Regionator
{

	#region Class: TestClassInRegion

	class TestClassInRegion
	{

	}

	#endregion

	#region Class: TestClassInRegion2

	internal class TestClassInRegion2
	{

	}

	#endregion

}
namespace Regionator
{

	#region Class: SourceTestClassWithoutRegion

	[TestFixture]
	class SourceTestClassWithoutRegion
	{
	}

	#endregion

	#region Interface: SourceTestInterfaceWithoutRegion

	/// <summary>
	/// SourceTestInterfaceWithoutRegion doc
	/// </summary>
	interface SourceTestInterfaceWithoutRegion
	{
	}

	#endregion

	#region Struct: SourceTestStructWithoutRegion

	struct SourceTestStructWithoutRegion
	{
	}

	#endregion

	#region Enum: SourceTestEnumWithoutRegion

	enum SourceTestEnumWithoutRegion
	{
	}

	#endregion

	#region Class: NotInRegionButRegionExists
	#endregion

	#region Class: NotInRegionButRegionExists

	class NotInRegionButRegionExists
	{
	}

	#endregion

}
namespace XmlDoc
{

	#region Class: ClasWithDoc

	/// <summary>
	/// Class With doc.
	/// </summary>
	public class ClasWithDoc
	{
	}

	#endregion

}
namespace Nested
{

	#region Class: RootType

	public class RootType
	{

		#region Class: NestedType

		public class NestedType
		{

			#region Class: DoubleNestedType

			class DoubleNestedType
			{
			}

			#endregion

		}

		#endregion

	}

	#endregion

}