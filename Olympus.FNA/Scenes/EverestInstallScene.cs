using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Olympus {
    public class EverestInstallScene : Scene {

        public override Element Generate()
            => new Group() {
                Layout = {
                    Layouts.Fill(),
                    Layouts.Row()
                },
                Children = {

                    new Group() {

                    },

                }
            };

    }

}
