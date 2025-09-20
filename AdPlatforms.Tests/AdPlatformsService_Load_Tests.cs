using AdPlatforms.Services;

namespace AdPlatforms.Tests
{
    public class AdPlatformsService_Load_Tests
    {
        private static (AdPlatformsService svc, AdPlatformsStore store) MakeSvc()
        {
            var store = new AdPlatformsStore();
            var svc = new AdPlatformsService(store);
            return (svc, store);
        }

        [Fact]
        public void Load_Simple_Succeeds_And_Counts()
        {
            var (svc, store) = MakeSvc();
            var input = """
            Яндекс.Директ:/ru
            Крутая реклама:/ru/svrd
            Ревдинский рабочий:/ru/svrd/revda,/ru/svrd/pervik
            """;

            var res = svc.LoadFromText(input);

            Assert.True(res.IsSuccess);
            Assert.Equal(3, res.PlatformsCount);
            Assert.True(store.LocationKeyCount >= 3); // /ru, /ru/svrd, /ru/svrd/revda, /ru/svrd/pervik
            Assert.Empty(res.Errors);
        }

        [Fact]
        public void Load_Ignores_Bad_Lines_But_Does_Not_Crash()
        {
            var (svc, _) = MakeSvc();
            var input = """
            NoColonHere
            EmptyName:
            Good One:/ru
            WithSpacesInLoc:/ru msk
            AlsoGood:/ru/svrd
            """;

            var res = svc.LoadFromText(input);

            Assert.True(res.IsSuccess);
            Assert.Equal(2, res.PlatformsCount); // Good One + AlsoGood
            Assert.NotEmpty(res.Errors);          // invalid lines reported
        }

        [Fact]
        public void Load_EmptyBody_Fails()
        {
            var (svc, _) = MakeSvc();
            var res = svc.LoadFromText("");

            Assert.False(res.IsSuccess);
            Assert.Contains(res.Errors, e => e.Contains("Empty body", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Load_NoValidEntries_Fails()
        {
            var (svc, _) = MakeSvc();
            var res = svc.LoadFromText("""
            Bad
            AnotherBad:
            NameWithSpaces:/ru msk
            """);

            Assert.False(res.IsSuccess);
            Assert.Contains(res.Errors, e => e.Contains("No valid entries parsed", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Load_Deduplicates_Platform_Per_Location()
        {
            var (svc, store) = MakeSvc();
            var res = svc.LoadFromText("""
            Dup:/ru
            Dup:/ru
            Another:/ru
            """);

            Assert.True(res.IsSuccess);
            var snapshot = store.Snapshot;
            Assert.True(snapshot.TryGetValue("/ru", out var set));
            Assert.Equal(2, set!.Count); // { Dup, Another } (no duplicates)
        }
    }
}