using AdPlatforms.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdPlatforms.Tests
{
    public class AdPlatformsService_Query_Tests
    {
        private static AdPlatformsService Seeded(out AdPlatformsStore store)
        {
            store = new AdPlatformsStore();
            var svc = new AdPlatformsService(store);
            svc.LoadFromText("""
            Яндекс.Директ:/ru
            Ревдинский рабочий:/ru/svrd/revda,/ru/svrd/pervik
            Газета уральских москвичей:/ru/msk,/ru/permobl,/ru/chelobl
            Крутая реклама:/ru/svrd
            """);
            return svc;
        }

        [Fact]
        public void Query_Exact_Leaf_Location_Returns_All_Ancestors()
        {
            var svc = Seeded(out _);

            var result = svc.FindByLocation("/ru/svrd/revda");

            // Alphabetical ordering by API design
            Assert.Equal(new[]
            {
            "Крутая реклама",
            "Ревдинский рабочий",
            "Яндекс.Директ"
        }, result);
        }

        [Fact]
        public void Query_Region_Returns_Appropriate()
        {
            var svc = Seeded(out _);

            var msk = svc.FindByLocation("/ru/msk");
            Assert.Equal(new[] { "Газета уральских москвичей", "Яндекс.Директ" }, msk);

            var svrd = svc.FindByLocation("/ru/svrd");
            Assert.Equal(new[] { "Крутая реклама", "Яндекс.Директ" }, svrd);
        }

        [Fact]
        public void Query_Root_Ru_Returns_Global_Only()
        {
            var svc = Seeded(out _);

            var res = svc.FindByLocation("/ru");

            Assert.Equal(new[] { "Яндекс.Директ" }, res);
        }

        [Fact]
        public void Query_Invalid_Location_Format_Returns_Empty()
        {
            var svc = Seeded(out _);

            Assert.Empty(svc.FindByLocation("ru"));       // missing leading '/'
            Assert.Empty(svc.FindByLocation("/ru msk"));  // space inside
            Assert.Empty(svc.FindByLocation(""));         // empty
            Assert.Empty(svc.FindByLocation("   "));      // whitespace
        }

        [Fact]
        public void Query_When_Store_Empty_Returns_Empty()
        {
            var store = new AdPlatformsStore();
            var svc = new AdPlatformsService(store);

            Assert.Empty(svc.FindByLocation("/ru/msk"));
        }

        [Fact]
        public void Query_Is_Case_Insensitive_For_Locations_And_Names()
        {
            var store = new AdPlatformsStore();
            var svc = new AdPlatformsService(store);
            svc.LoadFromText("""
            yandex:/RU
            local:/RU/svrd
            """);

            var res = svc.FindByLocation("/ru/SVRD");

            Assert.Equal(new[] { "local", "yandex" }, res);
        }

        [Fact]
        public void Query_Trims_And_Skip_Empty_Location_Parts_On_Load()
        {
            var store = new AdPlatformsStore();
            var svc = new AdPlatformsService(store);
            var load = svc.LoadFromText("Name:/ru/svrd  ,   ,   /ru/svrd/revda   ");

            Assert.True(load.IsSuccess);
            var res = svc.FindByLocation("/ru/svrd/revda");
            Assert.Equal(new[] { "Name" }, res);
        }
    }
}
