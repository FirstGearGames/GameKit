using FishNet.Object;
using GameKit.Examples.Inventories.Canvases;
using GameKit.Examples.Tooltips.Canvases;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Examples.Managers
{

    /// <summary>
    /// Gameplay canvases register to this manager.
    /// </summary>
    public class CanvasManager : NetworkBehaviour
    {
        #region Types.
        public class ResizeData
        {
            public byte Remaining;
            public ResizeDelegate Delegate;


            public ResizeData()
            {
                Remaining = 2;
            }

            public void Reset()
            {
                Remaining = 2;
                Delegate = null;
            }
        }
        #endregion

        #region Public.
        public delegate void ResizeDelegate();
        /// <summary>
        /// InventoryCanvas reference.
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public InventoryCanvas InventoryCanvas;
        /// <summary>
        /// TooltipCanvas reference.
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public TooltipCanvas TooltipCanvas;
        #endregion

        #region Private.
        /// <summary>
        /// Elements to resize.
        /// </summary>
        private List<ResizeData> _resizeDatas = new List<ResizeData>();
        /// <summary>
        /// Stack for ResizeData.
        /// </summary>
        private Stack<ResizeData> _resizeDatasStack = new Stack<ResizeData>();
        #endregion

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            base.NetworkManager.RegisterInstance(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            base.NetworkManager.UnregisterInstance<CanvasManager>();
        }

        private void Update()
        {
            Resize();
        }

        /// <summary>
        /// Calls pending resizeDatas.
        /// </summary>
        private void Resize()
        {
            for (int i = 0; i < _resizeDatas.Count; i++)
            {
                byte remaining = _resizeDatas[i].Remaining;
                _resizeDatas[i].Delegate?.Invoke();
                remaining--;
                if (remaining == 0)
                {
                    StoreResizeData(_resizeDatas[i]);
                    _resizeDatas.RemoveAt(i);
                    i--;
                }
                else
                {
                    _resizeDatas[i].Remaining = remaining;
                }
            }

        }

        /// <summary>
        /// Returns a ResizeData to use.
        /// </summary>
        /// <returns></returns>
        private ResizeData RetrieveResizeData()
        {
            return (_resizeDatasStack.Count > 0) ? _resizeDatasStack.Pop() : new ResizeData();
        }
        /// <summary>
        /// Stores a ResizeData.
        /// </summary>
        private void StoreResizeData(ResizeData rd)
        {
            rd.Reset();
            _resizeDatasStack.Push(rd);
        }
        /// <summary>
        /// Used to call a delegate twice, over two frames.
        /// This is an easy way to resize RectTransforms since they often will not update same frame.
        /// </summary>
        /// <param name="del"></param>
        public void Resize(ResizeDelegate del)
        {
            ResizeData rd = RetrieveResizeData();
            rd.Delegate = del;
            _resizeDatas.Add(rd);
        }
    }


}