using Microsoft.Xna.Framework;
using OlympUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Olympus {
    // Based off of CelesteNet's YamlHelper.
    public static class YamlHelper {

        public static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new ColorYamlTypeConverter())
            .Build();

        public static readonly ISerializer Serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
            .WithTypeConverter(new ColorYamlTypeConverter())
            .Build();

        /// <summary>
        /// Builds a deserializer that will provide YamlDotNet with the given object instead of creating a new one.
        /// This will make YamlDotNet update this object when deserializing.
        /// </summary>
        /// <param name="objectToBind">The object to set fields on</param>
        /// <returns>The newly-created deserializer</returns>
        public static IDeserializer DeserializerUsing(object objectToBind) {
            IObjectFactory defaultObjectFactory = new DefaultObjectFactory();
            Type objectType = objectToBind.GetType();

            return new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                // provide the given object if type matches, fall back to default behavior otherwise.
                .WithObjectFactory(type => type == objectType ? objectToBind : defaultObjectFactory.Create(type))
                .WithTypeConverter(new ColorYamlTypeConverter())
                .Build();
        }

    }
}
