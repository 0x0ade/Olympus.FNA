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
    public class ConfigurationScene : Scene {

        public override Element Generate()
            => new ScrollBox() {
                Layout = {
                    Layouts.Fill()
                },
                Content = new Group() {
                    Style = {
                        { "Padding", 8 },
                        { "Spacing", 32 }
                    },
                    Layout = {
                        Layouts.Fill(1, 0),
                        Layouts.Column()
                    },
                    Children = {

                        new Group() {
                            Style = {
                                { "Spacing", 16 },
                            },
                            Layout = {
                                Layouts.Fill(1, 0),
                                Layouts.Column()
                            },
                            Children = {
                                new HeaderMedium("Celeste Installations"),
                                new Group() {
                                    Layout = {
                                        Layouts.Fill(1, 0),
                                        Layouts.Row(false),
                                    },
                                    Children = {
                                        new Group() {
                                            Style = {
                                                { "Spacing", 8 },
                                            },
                                            Layout = {
                                                Layouts.Fill(1f / 2f, 0, 8, 0),
                                                Layouts.Column(),
                                            },
                                            Children = {
                                                new HeaderSmall("Managed by Olympus"),
                                                new Label("TODO"),
                                            }
                                        },

                                        new Group() {
                                            Style = {
                                                { "Spacing", 8 },
                                            },
                                            Layout = {
                                                Layouts.Fill(1f / 2f, 0, 8, 0),
                                                Layouts.Column(),
                                            },
                                            Children = {
                                                new HeaderSmall("Found on this PC"),
                                                new Label("TODO"),
                                            }
                                        },
                                    }
                                },
                            }
                        },

                        new Group() {
                            Style = {
                                { "Spacing", 8 }
                            },
                            Layout = {
                                Layouts.Fill(1, 0),
                                Layouts.Column()
                            },
                            Children = {
                                new HeaderMedium("Options"),
                                new Group() {
                                    Style = {
                                        { "Spacing", 8 }
                                    },
                                    Layout = {
                                        Layouts.Fill(1, 0),
                                        Layouts.Column()
                                    },
                                    Children = {
                                        new Label("TODO"),
                                        new Label("TODO"),
                                        new Label("TODO"),
                                        new Label("TODO"),
                                    }
                                },
                            }
                        },

                        new Group() {
                            Style = {
                                { "Spacing", 8 }
                            },
                            Layout = {
                                Layouts.Fill(1, 0),
                                Layouts.Column()
                            },
                            Children = {
                                new HeaderMedium("About Olympus"),
                                new Group() {
                                    Style = {
                                        { "Spacing", 8 }
                                    },
                                    Layout = {
                                        Layouts.Fill(1, 0),
                                        Layouts.Column()
                                    },
                                    Children = {
                                        new Label($"Version: {typeof(App).Assembly.GetName().Version} @ {Path.GetDirectoryName(typeof(App).Assembly.Location)}"),
                                        new Label($".NET Runtime: {Environment.Version} @ {Path.GetDirectoryName(typeof(object).Assembly.Location)}"),
                                        new Label($"System: {Environment.OSVersion} (using {NativeImpl.Native.GetType().Name})"),
                                        new Label($"Renderer: {FNAHooks.FNA3DDevice} (using {FNAHooks.FNA3DDriver})"),
                                    }
                                },
                            }
                        },

                    },
                }
            };

    }

}
