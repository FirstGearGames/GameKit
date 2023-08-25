using GameKit.Dependencies.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Dependencies.Canvases
{
    /// <summary>
    /// Used to track generic canvases and their states.
    /// </summary>
    public class CanvasManager : MonoBehaviour
    {
        /// <summary>
        /// Canvases which should block input.
        /// </summary>
        public IReadOnlyCollection<object> InputBlockingCanvases => _inputBlockingCanvases;
        private HashSet<object> _inputBlockingCanvases = new HashSet<object>();
        /// <summary>
        /// Canvases which are currently open, in the order they were opened.
        /// </summary>
        public IReadOnlyList<object> OpenCanvases => _openCanvases;
        private List<object> _openCanvases = new List<object>();
        /// <summary>
        /// True if any blocking canvas is open.
        /// </summary>
        public bool InputBlockingCanvasOpen => (_inputBlockingCanvases.Count > 0);

        /// <summary>
        /// Removes null references of canvases.
        /// This can be used as clean-up if you were unable to remove a canvas properly.
        /// Using this method regularly could be expensive if there are hundreds of open canvases.
        /// </summary>
        public void RemoveNullReferences()
        {
            //Open.
            for (int i = 0; i < _openCanvases.Count; i++)
            {
                if (_openCanvases[i] == null)
                {
                    _openCanvases.RemoveAt(i);
                    i--;
                }    
            }

            //Blocking.
            HashSet<object> hashset = CollectionCaches<object>.RetrieveHashSet();
            foreach (object item in _inputBlockingCanvases)
            {
                if (item != null)
                    hashset.Add(item);
            }
            //Clear original then re-add.
            _inputBlockingCanvases.Clear();
            foreach (object item in hashset)
                _inputBlockingCanvases.Add(item);
            CollectionCaches<object>.Store(hashset);
        }

        /// <summary>
        /// Returns true if canvas is an open canvas.
        /// </summary>
        public bool IsOpenCanvas(object canvas)
        {
            return _openCanvases.Contains(canvas);
        }
        /// <summary>
        /// Returns if the canvas is an input blocking canvas.
        /// </summary>
        public bool IsInputBlockingCanvas(object canvas)
        {
            return _inputBlockingCanvases.Contains(canvas);
        }

        /// <summary>
        /// Adds a canvas to OpenCanvases if not already added.
        /// </summary>
        /// <param name="addToBlocking">True to also add as an input blocking canvas.</param>
        /// <returns>True if the canvas was added, false if already added.</returns>
        public bool AddOpenCanvas(object canvas, bool addToBlocking)
        {
            bool added = _openCanvases.AddUnique(canvas);
            if (added && addToBlocking)
                _inputBlockingCanvases.Add(canvas);

            return added;
        }

        /// <summary>
        /// Removes a canvas from OpenCanvases.
        /// </summary>
        /// <returns>True if the canvas was removed, false if it was not added.</returns>
        public bool RemoveOpenCanvas(object canvas)
        {
            _inputBlockingCanvases.Remove(canvas);
            return _openCanvases.Remove(canvas);
        }


    }


}