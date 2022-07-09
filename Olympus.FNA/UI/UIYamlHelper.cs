using Microsoft.Xna.Framework;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace OlympUI {
    // Based off of CelesteNet's YamlHelper.
    public static class UIYamlHelper {

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

    public class ColorYamlTypeConverter : IYamlTypeConverter {

        public bool Accepts(Type type)
            => type == typeof(Color);

        public object ReadYaml(IParser parser, Type type) {
            Color c = new(0, 0, 0, 255);

            if (parser.TryConsume(out MappingStart _)) {
                do {
                    switch (parser.Consume<Scalar>().Value.ToUpperInvariant()) {
                        case "R":
                            c.R = byte.Parse(parser.Consume<Scalar>().Value);
                            break;

                        case "G":
                            c.G = byte.Parse(parser.Consume<Scalar>().Value);
                            break;

                        case "B":
                            c.B = byte.Parse(parser.Consume<Scalar>().Value);
                            break;

                        case "A":
                            c.A = byte.Parse(parser.Consume<Scalar>().Value);
                            break;
                    }
                } while (!parser.TryConsume(out MappingEnd _));
                return c;
            }

            return UIMath.ParseHexColor(parser.Consume<Scalar>().Value);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type) {
            Color c = (Color) (value ?? Color.White);

            /*
            emitter.Emit(new MappingStart());
            emitter.Emit(new Scalar("R"));
            emitter.Emit(new Scalar(c.R.ToString()));
            emitter.Emit(new Scalar("G"));
            emitter.Emit(new Scalar(c.G.ToString()));
            emitter.Emit(new Scalar("B"));
            emitter.Emit(new Scalar(c.B.ToString()));
            emitter.Emit(new Scalar("A"));
            emitter.Emit(new Scalar(c.A.ToString()));
            emitter.Emit(new MappingEnd());
            return;
            */

            emitter.Emit(new Scalar(c.ToHexString()));
        }

    }
}
