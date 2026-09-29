// RF-08: marca en la TV la opción con foco de un menú que se maneja con el celular
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace MoviMente.UI
{
    internal sealed class MenuView
    {
        private const string FocusClass = "foco";

        private readonly List<VisualElement> options;
        private readonly FocusList focus;

        // Cada opción es su contenedor .halo: el USS dibuja el foco a partir de ahí.
        public MenuView(VisualElement menu)
        {
            options = menu.Query(className: "halo").ToList();
            focus = new FocusList(options.Count);
            Paint();
        }

        public int Index => focus.Index;

        public void Reset(int index)
        {
            focus.Reset(index);
            Paint();
        }

        public bool Move(int step, float now)
        {
            if (!focus.Move(step, now)) return false;
            Paint();
            return true;
        }

        private void Paint()
        {
            for (int i = 0; i < options.Count; i++) options[i].EnableInClassList(FocusClass, i == focus.Index);
        }
    }
}
