﻿/* 
* Copyright (c) Marc Clifton
* The Code Project Open License (CPOL) 1.02
* http://www.codeproject.com/info/cpol10.aspx
*/

using System.Drawing;

namespace FlowSharpLib
{
    [ToolboxOrder(2)]
    public class Ellipse : GraphicElement
    {
        public Ellipse(Canvas canvas) : base(canvas)
        {
			HasCornerConnections = false;
		}

		public override void Draw(Graphics gr, bool showSelection = true)
        {
            gr.FillEllipse(FillBrush, ZoomRectangle);
            gr.DrawEllipse(BorderPen, ZoomRectangle);
            base.Draw(gr, showSelection);
        }
    }
}
