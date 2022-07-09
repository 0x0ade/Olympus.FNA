using OlympUI;

namespace Olympus {
    public class EverestInstallScene : Scene {

        public override Element Generate()
            => new Group() {
                Layout = {
                    Layouts.Fill(),
                    Layouts.Row(false)
                },
                Children = {

                    new Group() {

                    },

                }
            };

    }

}
