﻿using Neo4jClient.DataAnnotations.Cypher;
using Neo4jClient.DataAnnotations.Tests.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace Neo4jClient.DataAnnotations.Tests
{
    public class UtilitiesTests
    {
        public static List<object[]> VarsData = new List<object[]>()
        {
            new object[] { (Expression<Func<ActorNode>>)(() => Vars.Get<ActorNode>("actor")), "actor" },

            new object[] { (Expression<Func<int>>)(() => (int)Vars.Get("actor")["address_location_latitude"]),
                "actor.address_location_latitude" },

            new object[] { (Expression<Func<string>>)(() => ((string[])Vars.Get("actor")["roles"])[0]),
                "actor.roles[0]" },

            new object[] { (Expression<Func<string>>)(() => (Vars.Get("actor")["roles"] as string[])[0]),
                "actor.roles[0]" },

            new object[] { (Expression<Func<string>>)(() => Vars.Get<ActorNode>("actor").Roles.ElementAt(2)),
                "actor.Roles[2]" },

            new object[] { (Expression<Func<Location>>)(() => (Vars.Get<ActorNode>("actor").Address as AddressWithComplexType).Location),
                "actor.NewAddressName.Location" },

            new object[] { (Expression<Func<double>>)(() => (Vars.Get<ActorNode>("actor").Address as AddressWithComplexType).Location.Latitude),
                "actor.NewAddressName_Location_Latitude" },

            //recursive params
            new object[] { (Expression<Func<string>>)(() => (Vars.Get("actor")["roles"] as string[])[(Vars.Get("actor2")["roles"] as string[])[2].As<int>()]),
                "actor.roles[actor2.roles[2]]" },

            //2 levels recursion
            new object[] { (Expression<Func<string>>)(() => (Vars.Get("actor")["roles"].As<string[]>())[
                (Vars.Get("actor2")["roles"] as string[])[Vars.Get<ActorNode>("actor3").Born
                    ].As<int>()]),
                "actor.roles[actor2.roles[actor3.Born]]" }
        };

        [Theory]
        [MemberData("VarsData", MemberType = typeof(UtilitiesTests))]
        public void VarsSerializationWithConverter<T>(Expression<Func<T>> expression,
            string expectedText, bool useResolvedJsonName = true)
        {
            TestUtilities.AddEntityTypes();

            var retrievedMembers = Utilities.GetSimpleMemberAccessStretch(expression.Body, out var val);

            Assert.Equal(true, Utilities.HasVars(retrievedMembers));

            var varText = Utilities
                .BuildVars(retrievedMembers, null, TestUtilities.SerializeWithConverter,
                out var typeReturned, useResolvedJsonName: useResolvedJsonName);

            Assert.Equal(expectedText, varText);
            Assert.Equal(typeof(T), typeReturned);
        }

        [Theory]
        [MemberData("VarsData", MemberType = typeof(UtilitiesTests))]
        public void VarsSerializationWithResolver<T>(Expression<Func<T>> expression,
            string expectedText, bool useResolvedJsonName = true)
        {
            TestUtilities.AddEntityTypes();

            var retrievedMembers = Utilities.GetSimpleMemberAccessStretch(expression.Body, out var val);

            Assert.Equal(true, Utilities.HasVars(retrievedMembers));

            var varText = Utilities
                .BuildVars(retrievedMembers, TestUtilities.Resolver, null,
                out var typeReturned, useResolvedJsonName: useResolvedJsonName);

            Assert.Equal(expectedText, varText);
            Assert.Equal(typeof(T), typeReturned);
        }

        public static List<object[]> FinalPropertiesData = new List<object[]>()
        {
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "1671" },
                    { "NewAddressName_AddressLine", "ellenPompeo.NewAddressName_AddressLine" },
                    { "NewAddressName_City", "ellenPompeo.NewAddressName_City" },
                    { "NewAddressName_State", "ellenPompeo.NewAddressName_State" },
                    { "NewAddressName_Country", "ellenPompeo.NewAddressName_Country" },
                },
                (Expression<Func<object>>)(() => new Dictionary<string, object>()
                {
                    { "Name", TestUtilities.Actor.Name },
                    { "Born", TestUtilities.Actor.Born },
                    { "Address", TestUtilities.Actor.Address }
                }.Set(a => a["Address"] == Vars.Get<ActorNode>("ellenPompeo").Address
                    && (int)a["Born"] == 1671 && a["Name"] == "Shonda Rhimes")),
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "1969" },
                    { "NewAddressName_AddressLine", "shondaRhimes.NewAddressName_AddressLine" },
                    { "NewAddressName_City", "null" },
                    { "NewAddressName_State", "null" },
                    { "NewAddressName_Country", "null" },
                    { "NewAddressName_Location_Latitude", "4.0" },
                    { "NewAddressName_Location_Longitude", "ellenPompeo.NewAddressName_Location_Longitude" },
                },
                (Expression<Func<object>>)(() => new { TestUtilities.NormalAddressActor.Name, TestUtilities.NormalAddressActor.Born, TestUtilities.NormalAddressActor.Address }
                .Set(a => a.Address == new AddressWithComplexType()
                {   //Use this style only if you're sure all the properties here are assigned, 
                    //because this address object would replace the instance address property entirely.
                    //Also note that there's a good chance the parameters set inline here wouldn't make it to the generated pattern.
                    //This was done mainly for testing. 
                    //Use a => a.Address.Location.Longitude == (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"] instead.

                    AddressLine = Vars.Get<ActorNode>("shondaRhimes").Address.AddressLine,
                    Location = new Location()
                    {
                        Latitude = 4.0,
                        Longitude = (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"]
                    }
                } && a.Name == "Shonda Rhimes"))
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "1969" },
                    { "NewAddressName_AddressLine", "shondaRhimes.NewAddressName_AddressLine" },
                    { "NewAddressName_City", "null" },
                    { "NewAddressName_State", "null" },
                    { "NewAddressName_Country", "null" },
                    { "NewAddressName_Location_Latitude", "4.0" },
                    { "NewAddressName_Location_Longitude", "ellenPompeo.NewAddressName_Location_Longitude" },
                },
                (Expression<Func<object>>)(() => new { TestUtilities.Actor.Name, TestUtilities.Actor.Born, Address = TestUtilities.Actor.Address }
                .Set(a => a.Address == new AddressWithComplexType()
                {   //Use this style only if you're sure all the properties here are assigned, 
                    //because this address object would replace the instance address property entirely.
                    //Also note that there's a good chance the parameters set inline here wouldn't make it to the generated pattern.
                    //This was done mainly for testing. 
                    //Use a => a.Address.Location.Longitude == (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"] instead.

                    AddressLine = Vars.Get<ActorNode>("shondaRhimes").Address.AddressLine,
                    Location = new Location()
                    {
                        Latitude = 4.0,
                        Longitude = (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"]
                    }
                } && a.Name == "Shonda Rhimes"))
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "1969" },
                    { "NewAddressName_AddressLine", "null" },
                    { "NewAddressName_City", "\"Los Angeles\"" },
                    { "NewAddressName_State", "\"California\"" },
                    { "NewAddressName_Country", "\"US\"" },
                    { "NewAddressName_Location_Latitude", "34.0522" },
                    { "NewAddressName_Location_Longitude", "ellenPompeo.NewAddressName_Location_Longitude" },
                },
                (Expression<Func<object>>)(() => new { TestUtilities.Actor.Name, TestUtilities.Actor.Born, Address = TestUtilities.Actor.Address as AddressWithComplexType }
                .Set(a => a.Address.Location.Longitude == new AddressWithComplexType()
                {   //Using this style, variables set inline of a member access may or may not make it to the generated pattern, or even throw an exception.
                    //This is because this MemberInit may be taken as an object value, since it was accessed, and then used directly.
                    //This was done mainly for testing. 
                    //Use a => a.Address.Location.Longitude == (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"] instead.

                    AddressLine = Vars.Get<ActorNode>("shondaRhimes").Address.AddressLine,
                    Location = new Location()
                    {
                        Latitude = 4.0,
                        Longitude = (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"]
                    }
                }.Location.Longitude && a.Name == "Shonda Rhimes")),
                typeof(InvalidOperationException), string.Format(Messages.AmbiguousVarsPathError, "shondaRhimes.NewAddressName_AddressLine")
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "1969" },
                    { "NewAddressName_AddressLine", "shondaRhimes.NewAddressName_AddressLine" },
                    { "NewAddressName_City", "null" },
                    { "NewAddressName_State", "null" },
                    { "NewAddressName_Country", "null" },
                    { "NewAddressName_Location_Latitude", "4.0" },
                    { "NewAddressName_Location_Longitude", "ellenPompeo.NewAddressName_Location_Longitude" },
                },
                (Expression<Func<object>>)(() => new { TestUtilities.Actor.Name, TestUtilities.Actor.Born, Address = TestUtilities.Actor.Address as AddressWithComplexType }
                .Set(a => a.Address == new AddressWithComplexType()
                {   //Use this style only if you're sure all the properties here are assigned, 
                    //because this address object would replace the instance address property entirely.
                    //Also note that there's a good chance the parameters set inline here wouldn't make it to the generated pattern.
                    //This was done mainly for testing. 
                    //Use a => a.Address.Location.Longitude == (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"] instead.

                    AddressLine = Vars.Get<ActorNode>("shondaRhimes").Address.AddressLine,
                    Location = new Location()
                    {
                        Latitude = 4.0,
                        Longitude = (double)Vars.Get("ellenPompeo")["NewAddressName_Location_Longitude"]
                    }
                } && a.Name == "Shonda Rhimes"))
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "1969" },
                    { "NewAddressName_AddressLine", "shondaRhimes.NewAddressName_AddressLine" },
                    { "NewAddressName_City", "\"Los Angeles\"" },
                    { "NewAddressName_State", "\"California\"" },
                    { "NewAddressName_Country", "\"US\"" },
                    { "NewAddressName_Location_Latitude", "34.0522" },
                    { "NewAddressName_Location_Longitude", "-118.2437" },
                },
                (Expression<Func<object>>)(() => new { TestUtilities.Actor.Name, TestUtilities.Actor.Born, TestUtilities.Actor.Address }
                .Set(a => (a.Address as AddressWithComplexType).AddressLine == Vars.Get<ActorNode>("shondaRhimes").Address.AddressLine && a.Name == "Shonda Rhimes"))
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "1969" },
                    { "NewAddressName_AddressLine", "shondaRhimes.NewAddressName_AddressLine" },
                    { "NewAddressName_City", "\"Los Angeles\"" },
                    { "NewAddressName_State", "\"California\"" },
                    { "NewAddressName_Country", "\"US\"" },
                    { "NewAddressName_Location_Latitude", "34.0522" },
                    { "NewAddressName_Location_Longitude", "-118.2437" },
                },
                (Expression<Func<object>>)(() => new { TestUtilities.Actor.Name, TestUtilities.Actor.Born, TestUtilities.Actor.Address }
                .Set(a => a.Address.AddressLine == Vars.Get<ActorNode>("shondaRhimes").Address.AddressLine && a.Name == "Shonda Rhimes"))
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Shonda Rhimes\"" },
                    { "Born", "ellenPompeo.Born" },
                    { "Roles", "null"},
                    { "NewAddressName_AddressLine", "null" },
                    { "NewAddressName_City", "\"Los Angeles\"" },
                    { "NewAddressName_State", "\"California\"" },
                    { "NewAddressName_Country", "\"US\"" },
                    { "NewAddressName_Location_Latitude", "34.0522" },
                    { "NewAddressName_Location_Longitude", "-118.2437" },
                    { "TestForeignKeyId", "0" },
                    { "TestMarkedFK", "0" },
                    { "TestGenericForeignKeyId", "null" },
                },
                (Expression<Func<object>>)(() => TestUtilities.Actor.Set(a => a.Born == Vars.Get<ActorNode>("ellenPompeo").Born && a.Name == "Shonda Rhimes"))
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Address", TestUtilities.Actor.Address },
                    { "Coordinates", $"[{(TestUtilities.Actor.Address as AddressWithComplexType).Location.Latitude},shondaRhimes.NewAddressName_Location_Longitude]" }
                },
                (Expression<Func<object>>)(() => new
                {
                    //the following is purely a test, and not necessarily a good example for neo4j cypher.
                    Address = (TestUtilities.Actor.Address as AddressWithComplexType)._(),
                    Coordinates = new double[] { (TestUtilities.Actor.Address as AddressWithComplexType).Location.Latitude,
                        (double)Vars.Get("shondaRhimes")["NewAddressName_Location_Longitude"] }
                })
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Location_Latitude", "A.Location_Latitude" },
                    { "Location_Longitude", "56.9" }
                },
                (Expression<Func<object>>)(() => new
                {
                    //the following is purely a test, and not necessarily a good example for neo4j cypher.
                    //avoid using inline Vars with outer member access.
                    new AddressWithComplexType()
                    {
                        AddressLine = Vars.Get("A")["AddressLine"] as string,
                        Location = new Location()
                        {
                            Latitude = (double)Vars.Get("A")["Location_Latitude"],
                            Longitude = 56.90
                        }
                    }.Location
                }), typeof(InvalidOperationException), string.Format(Messages.AmbiguousVarsPathError, "A.AddressLine")
            },
            new object[] {
                new Dictionary<string, dynamic>()
                {
                    { "Name", "\"Ellen Pompeo\"" },
                    { "Born", "shondaRhimes.Born" },
                    { "Roles", "[\"Meredith Grey\"]" },
                    { "Age", "\"47\"" }, //because we assigned a 47 string and not a 47 int.
                },
                (Expression<Func<object>>)(() =>
                new
                {
                    Name = "Ellen Pompeo",
                    Born = Vars.Get<ActorNode>("shondaRhimes").Born,
                    Roles = new string[] { "Meredith Grey" },
                    Age = 47.ToString()
                })
            },
        };

        [Theory]
        [MemberData("FinalPropertiesData", MemberType = typeof(UtilitiesTests))]
        public void FinalPropertiesResolutionWithConverter(Dictionary<string, dynamic> expected, LambdaExpression expression,
            Type expectedExceptionType = null, string expectedExceptionMessage = null)
        {
            TestUtilities.AddEntityTypes();

            Func<JObject> action = () => Utilities.GetFinalProperties
                    (expression, null, TestUtilities.Converter, TestUtilities.SerializeWithConverter, out var hasVars);

            FinalPropertiesResolution(action, TestUtilities.SerializeWithConverter,
                expected, expectedExceptionType, expectedExceptionMessage);
        }

        [Theory]
        [MemberData("FinalPropertiesData", MemberType = typeof(UtilitiesTests))]
        public void FinalPropertiesResolutionWithResolver(Dictionary<string, dynamic> expected, LambdaExpression expression,
            Type expectedExceptionType = null, string expectedExceptionMessage = null)
        {
            TestUtilities.AddEntityTypes();

            Func<JObject> action = () => Utilities.GetFinalProperties
                    (expression, TestUtilities.Resolver, null, TestUtilities.SerializeWithResolver, out var hasVars);

            FinalPropertiesResolution(action, TestUtilities.SerializeWithResolver,
                expected, expectedExceptionType, expectedExceptionMessage);
        }

        private void FinalPropertiesResolution(Func<JObject> action, Func<object, string> serializer,
            Dictionary<string, dynamic> expected, Type expectedExceptionType = null,
            string expectedExceptionMessage = null)
        {
            TestUtilities.AddEntityTypes();

            if (expectedExceptionType != null)
            {
                var exception = Assert.Throws(expectedExceptionType, () => action.Invoke());

                if (expectedExceptionMessage != null)
                    Assert.Equal(expectedExceptionMessage, exception.Message);
            }
            else
            {
                var finalPropsObj = action.Invoke();
                TestUtilities.TestFinalPropertiesForEquality(serializer, expected, finalPropsObj);
            }
        }
    }
}
