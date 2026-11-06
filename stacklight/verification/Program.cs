using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 2)
{
    Console.Error.WriteLine(
        "Usage: StacklightVerification STACKLIGHT_DLL GAME_MANAGED_PATH"
    );
    return 2;
}

string stacklightPath = Path.GetFullPath(args[0]);
string managedPath = Path.GetFullPath(args[1]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string candidate = Path.Combine(managedPath, name.Name + ".dll");
    return File.Exists(candidate)
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate)
        : null;
};

Assembly assembly =
    AssemblyLoadContext.Default.LoadFromAssemblyPath(stacklightPath);
Type systemType = assembly.GetType(
    "Stacklight.StacklightUISystem",
    throwOnError: true
)!;
const BindingFlags Flags =
    BindingFlags.NonPublic | BindingFlags.Static;
MethodInfo sanitize = systemType.GetMethod("Sanitize", Flags)!;
MethodInfo filter = systemType.GetMethod(
    "IsForeignSavedMapRecord",
    Flags
)!;
MethodInfo startActiveModsQuery = systemType.GetMethod(
    "TryStartActiveModsQuery",
    Flags
)!;

string assetReference =
    "assetdb://user/Saves/123/Sample%20Map.cok@" +
    "SampleMapTerrain.Prefab";

Type modType = AssemblyLoadContext.Default
    .LoadFromAssemblyPath(Path.Combine(managedPath, "Colossal.PSI.Common.dll"))
    .GetType("Colossal.PSI.Common.Mod", throwOnError: true)!;
Type platformType = AssemblyLoadContext.Default
    .LoadFromAssemblyPath(Path.Combine(managedPath, "Colossal.PSI.PdxSdk.dll"))
    .GetType("Colossal.PSI.PdxSdk.PdxSdkPlatform", throwOnError: true)!;
Type? installedResult = platformType.GetMethod("GetModsInActivePlaysetAsync", Type.EmptyTypes)
    ?.ReturnType.GetProperty("Result")?.PropertyType;
object CreatePlatform(Type apiType) =>
    Activator.CreateInstance(apiType.MakeGenericType(modType))!;
object?[] asyncQueryArguments = { CreatePlatform(typeof(AsyncPlaysetApi<>)), null, null };
bool asyncQueryStarted = (bool)startActiveModsQuery.Invoke(
    null,
    asyncQueryArguments
)!;
object?[] legacyQueryArguments = { CreatePlatform(typeof(LegacyPlaysetApi<>)), null, null };
bool legacyQueryStarted = (bool)startActiveModsQuery.Invoke(
    null,
    legacyQueryArguments
)!;
object?[] missingQueryArguments = { new MissingPlaysetApi(), null, null };
bool missingQueryStarted = (bool)startActiveModsQuery.Invoke(
    null,
    missingQueryArguments
)!;

var checks = new Dictionary<string, bool>
{
    ["real email is redacted"] =
        (string)sanitize.Invoke(
            null,
            new object[] { "contact person@example.com" }
        )! == "contact [REDACTED EMAIL]",
    ["assetdb prefab reference is preserved"] =
        (string)sanitize.Invoke(
            null,
            new object[] { assetReference }
        )! == assetReference,
    ["foreign saved-map record is suppressed"] =
        (bool)filter.Invoke(
            null,
            new object[] { assetReference, "", "International 11" }
        )!,
    ["current saved-map record is retained"] =
        !(bool)filter.Invoke(
            null,
            new object[] { assetReference, "", "Sample Map" }
        )!,
    ["global mod record is retained"] =
        !(bool)filter.Invoke(
            null,
            new object[]
            {
                "A global mod failure",
                "",
                "Sample Map"
            }
        )!,
    ["saved-map record is hidden with no map loaded"] =
        (bool)filter.Invoke(
            null,
            new object[] { assetReference, "", "" }
        )!,
    ["current async playset API is selected"] =
        asyncQueryStarted && asyncQueryArguments[1] is Task,
    ["installed async API returns a mod collection"] =
        installedResult != null && typeof(IEnumerable<>).MakeGenericType(modType)
            .IsAssignableFrom(installedResult),
    ["legacy sync playset API remains supported"] =
        legacyQueryStarted && legacyQueryArguments[1] is null &&
        legacyQueryArguments[2] is System.Collections.IEnumerable,
    ["missing playset API fails without throwing"] = !missingQueryStarted
};

foreach ((string name, bool passed) in checks)
{
    Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {name}");
}

return checks.Values.All(value => value) ? 0 : 1;

internal sealed class AsyncPlaysetApi<T>
{
    public Task<HashSet<T>> GetModsInActivePlaysetAsync()
    {
        return Task.FromResult(new HashSet<T>());
    }
}

internal sealed class LegacyPlaysetApi<T>
{
    // A newer method with an incompatible result must not mask the legacy API.
    public Task GetModsInActivePlaysetAsync() => throw new InvalidOperationException();
    public List<T> GetModsInActivePlaysetSync() => new List<T>();
}

internal sealed class MissingPlaysetApi
{
}
