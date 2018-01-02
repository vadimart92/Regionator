using System;
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

	[TestFixture]
	class SourceTestClassWithoutRegion
	{
	}

	interface SourceTestInterfaceWithoutRegion
	{
	}

	struct SourceTestInterfaceWithoutRegion
	{
	}

	enum SourceTestInterfaceWithoutRegion
	{
	}
}

