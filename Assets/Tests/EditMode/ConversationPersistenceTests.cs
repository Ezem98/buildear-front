using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BuildeAR.Tests.EditMode
{
    public class ConversationPersistenceTests
    {
        [Test]
        public void NewtonsoftPersistence_RoundTripsRootConversationList()
        {
            Type controllerType = FindType("UIController");
            Type conversationType = FindType("ConversationData");
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(
                conversationType
            );
            IList conversations = (IList)Activator.CreateInstance(listType);
            object conversation = Activator.CreateInstance(conversationType);
            conversationType.GetField("id").SetValue(conversation, 42);
            conversationType.GetField("user_id").SetValue(conversation, 7);
            conversations.Add(conversation);

            MethodInfo serialize = controllerType.GetMethod(
                "SerializeConversations",
                BindingFlags.Public | BindingFlags.Static
            );
            MethodInfo deserialize = controllerType.GetMethod(
                "DeserializeConversations",
                BindingFlags.Public | BindingFlags.Static
            );
            string json = (string)serialize.Invoke(null, new object[] { conversations });
            IList restored = (IList)deserialize.Invoke(null, new object[] { json });

            Assert.That(json, Does.StartWith("["));
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(
                conversationType.GetField("id").GetValue(restored[0]),
                Is.EqualTo(42)
            );
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{}")]
        [TestCase("not-json")]
        public void InvalidLegacyConversationData_ReturnsEmptyList(string json)
        {
            Type controllerType = FindType("UIController");
            MethodInfo deserialize = controllerType.GetMethod(
                "DeserializeConversations",
                BindingFlags.Public | BindingFlags.Static
            );
            IList restored = (IList)deserialize.Invoke(null, new object[] { json });

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Count, Is.Zero);
        }

        private static Type FindType(string typeName)
        {
            Type type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(candidate => candidate != null);

            Assert.That(type, Is.Not.Null, $"No se encontró el tipo {typeName}.");
            return type;
        }
    }
}
