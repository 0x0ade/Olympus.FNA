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
    public class HomeScene : Scene {

        public override Element Generate()
            => new Group() {
                Style = {
                    { "Padding", 8 },
                },
                Layout = {
                    Layouts.Fill(1, 1),
                },
                Children = {
                        
                    new Group() {
                        Style = {
                            { "Spacing", 16 },
                        },
                        Layout = {
                            Layouts.Fill(1f, 0.4f, 0, 32),
                            Layouts.Top(),
                            Layouts.Left(),
                            Layouts.Column(),
                        },
                        Children = {
                            new Group() {
                                Layout = {
                                    Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                                    Layouts.Row(),
                                },
                                Children = {

                                    new Panel() {
                                        Layout = {
                                            Layouts.Fill(1f / 3f, 1, 8 / 2, 0),
                                            Layouts.Move(8 * 0, 0),
                                            Layouts.Column(),
                                        },
                                        Children = {
                                            new HeaderSmall("Cool Mod"),
                                            new Label("TODO"),
                                        }
                                    },

                                    new Panel() {
                                        Layout = {
                                            Layouts.Fill(1f / 3f, 1, 8, 0),
                                            Layouts.Move(8 * 1, 0),
                                            Layouts.Column(),
                                        },
                                        Children = {
                                            new HeaderSmall("Popular Mod"),
                                            new Label("TODO"),
                                        }
                                    },

                                    new Panel() {
                                        Layout = {
                                            Layouts.Fill(1f / 3f, 1, 8 / 2, 0),
                                            Layouts.Move(8 * 2, 0),
                                            Layouts.Column(),
                                        },
                                        Children = {
                                            new HeaderSmall("Some Mod"),
                                            new Label("TODO"),
                                        }
                                    },

                                }
                            },
                        }
                    },

                    new Group() {
                        Style = {
                            { "Spacing", 16 },
                        },
                        Layout = {
                            Layouts.Fill(0.7f, 0.6f, 32, 0),
                            Layouts.Bottom(),
                            Layouts.Left(),
                            Layouts.Column(),
                        },
                        Children = {
                            new HeaderMedium("Installed"),
                            new Group() {
                                Clip = true,
                                Cached = true,
                                CachePadding = 8,
                                Layout = {
                                    Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                                },
                                Children = {
                                    new ScrollBox() {
                                        Layout = {
                                            Layouts.Fill(1, 1)
                                        },
                                        Content = new Group() {
                                            Style = {
                                                { "Spacing", 8 },
                                            },
                                            Layout = {
                                                Layouts.Fill(1, 0),
                                                Layouts.Column(),
                                            },
                                            Children = {

                                                new Panel() {
                                                    Layout = {
                                                        Layouts.Fill(1, 0),
                                                        Layouts.Column(),
                                                    },
                                                    Children = {
                                                        new HeaderSmall("Everest"),
                                                        new HeaderSmaller("Version: 1.4.0.0-fna + 1.3102.0-azure-39c72"),
                                                        new Label("idk what this should say"),
                                                    }
                                                },

                                                new Panel() {
                                                    Layout = {
                                                        Layouts.Fill(1, 0),
                                                        Layouts.Column(),
                                                    },
                                                    Children = {
                                                        new HeaderSmall("Awesome Mod"),
                                                        new HeaderSmaller("Version: 1.0.0 (newest: 6.9.0)"),
                                                        new Label("Does Some Cool Things™️"),
                                                    }
                                                },

                                                new Panel() {
                                                    Layout = {
                                                        Layouts.Fill(1, 0),
                                                        Layouts.Column(),
                                                    },
                                                    Children = {
                                                        new HeaderSmall("Another Mod"),
                                                        new HeaderSmaller("Version: 4.2.0"),
                                                        new Label("idk what this should say"),
                                                    }
                                                },

                                            }
                                        }
                                    },
                                },
                            },
                        }
                    },

                    new Group() {
                        Style = {
                            { "Spacing", 16 },
                        },
                        Layout = {
                            Layouts.Fill(0.3f, 0.6f, 0, 0),
                            Layouts.Bottom(),
                            Layouts.Right(),
                            Layouts.Column(),
                        },
                        Children = {
                            new HeaderMedium("News"),
                            new Group() {
                                Clip = true,
                                Cached = true,
                                CachePadding = 8,
                                Layout = {
                                    Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                                },
                                Children = {
                                    new ScrollBox() {
                                        Layout = {
                                            Layouts.Fill(1, 1)
                                        },
                                        Content = new Group() {
                                            Style = {
                                                { "Spacing", 8 },
                                            },
                                            Layout = {
                                                Layouts.Fill(1, 0),
                                                Layouts.Column(),
                                            },
                                            Children = {

                                                new Panel() {
                                                    Layout = {
                                                        Layouts.Fill(1, 0),
                                                        Layouts.Column(),
                                                    },
                                                    Children = {
                                                        new HeaderSmall("Awesome News"),
                                                        new Label("TODO"),
                                                    }
                                                },

                                                new Panel() {
                                                    Layout = {
                                                        Layouts.Fill(1, 0),
                                                        Layouts.Column(),
                                                    },
                                                    Children = {
                                                        new HeaderSmall("Bad News"),
                                                        new Label("TODO"),
                                                    }
                                                },

                                            }
                                        }
                                    },
                                },
                            },
                        }
                    },

                },
            };

    }

}
