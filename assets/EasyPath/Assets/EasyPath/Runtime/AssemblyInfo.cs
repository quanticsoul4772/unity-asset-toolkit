using System.Runtime.CompilerServices;

// Expose internal members to test assemblies for testing implementation details
[assembly: InternalsVisibleTo("EasyPath.Tests.Editor")]
[assembly: InternalsVisibleTo("EasyPath.Tests.Runtime")]
