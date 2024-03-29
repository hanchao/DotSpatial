// ********************************************************************************************************
// Product Name: DotSpatial.Symbology.Forms.dll
// Description:  The core assembly for the DotSpatial 6.0 distribution.
// ********************************************************************************************************
// The contents of this file are subject to the MIT License (MIT)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is DotSpatial.dll
//
// The Initial Developer of this Original Code is Ted Dunsford. Created 4/28/2009 4:21:06 PM
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
//
// ********************************************************************************************************

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DotSpatial.Symbology.Forms
{
    /// <summary>
    /// DetailedLineSymbolDialog
    /// </summary>
    public partial class DetailedLineSymbolControl : UserControl
    {
        #region Events

        /// <summary>
        /// Fires an event indicating that changes should be applied.
        /// </summary>
        public event EventHandler ChangesApplied;

        /// <summary>
        /// Occurs when the user clicks the AddToCustomSymbols button
        /// </summary>
        public event EventHandler<LineSymbolizerEventArgs> AddToCustomSymbols;

        #endregion

        #region Private Variables

        private bool _ignoreChanges;
        private ILineSymbolizer _original;
        private ILineSymbolizer _symbolizer;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of DetailedLineSymbolDialog
        /// </summary>
        public DetailedLineSymbolControl()
        {
            _original = new LineSymbolizer();
            _symbolizer = new LineSymbolizer();
            Configure();
        }

        /// <summary>
        /// Creates a new Detailed Line Symbol Dialog that displays a copy of the original,
        /// and when apply changes is pressed, will copy properties to the original.
        /// </summary>
        /// <param name="original">The current symbolizer being viewed on the map</param>
        public DetailedLineSymbolControl(ILineSymbolizer original)
        {
            _original = original;
            _symbolizer = original.Copy();
            Configure();
        }

        /// <summary>
        /// This constructor shows a different symbolizer in the view from what is currently loaded on the map.
        /// If apply changes is clicked, the properties of the current symbolizer will be copied to the original.
        /// </summary>
        /// <param name="original">The symbolizer on the map</param>
        /// <param name="displayed">The symbolizer that defines the form setup</param>
        public DetailedLineSymbolControl(ILineSymbolizer original, ILineSymbolizer displayed)
        {
            _symbolizer = displayed;
            _original = original;
            Configure();
        }

        private void Configure()
        {
            InitializeComponent();

            ccStrokes.Strokes = _symbolizer.Strokes;
            ccStrokes.AddClicked += ccStrokes_AddClicked;
            ccStrokes.SelectedItemChanged += ccStrokes_SelectedItemChanged;
            ccStrokes.ListChanged += ccStrokes_ListChanged;

            ccDecorations.AddClicked += ccDecorations_AddClicked;
            ccDecorations.SelectedItemChanged += ccDecorations_SelectedItemChanged;
            ccDecorations.ListChanged += ccDecorations_ListChanged;

            lblPreview.Paint += lblPreview_Paint;
            lblDecorationPreview.Paint += lblDecorationPreview_Paint;
            dashControl1.PatternChanged += dashControl1_PatternChanged;
            dblWidthCartographic.TextChanged += dblWidthCartographic_TextChanged;
            cbColorCartographic.ColorChanged += cbColorCartographic_ColorChanged;
            cbColorSimple.ColorChanged += cbColorSimple_ColorChanged;

            cmbScaleMode.SelectedItem = _symbolizer.ScaleMode.ToString();
            chkSmoothing.Checked = _symbolizer.Smoothing;
            dblOffset.TextChanged += dblOffset_TextChanged;
            dblStrokeOffset.TextChanged += dblStrokeOffset_TextChanged;
            sldOpacitySimple.ValueChanged += sldOpacitySimple_ValueChanged;
            sldOpacityCartographic.ValueChanged += sldOpacityCartographic_ValueChanged;
            if (_symbolizer != null && _symbolizer.Strokes != null && _symbolizer.Strokes.Count > 0)
            {
                ccStrokes.SelectedStroke = _symbolizer.Strokes[0];
            }
        }

        private void radLineJoin_ValueChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            if (ccStrokes.SelectedStroke == null) return;
            IStroke stroke = ccStrokes.SelectedStroke;
            ICartographicStroke cs = stroke as ICartographicStroke;
            if (cs != null)
            {
                cs.JoinType = radLineJoin.Value;
            }
            UpdatePreview();
        }

        private void cbColorSimple_ColorChanged(object sender, EventArgs e)
        {
            SetColor(cbColorSimple.Color);
        }

        private void cbColorCartographic_ColorChanged(object sender, EventArgs e)
        {
            SetColor(cbColorCartographic.Color);
        }

        private void sldOpacityCartographic_ValueChanged(object sender, EventArgs e)
        {
            UpdateOpacity((float)sldOpacityCartographic.Value);
        }

        private void sldOpacitySimple_ValueChanged(object sender, EventArgs e)
        {
            UpdateOpacity((float)sldOpacitySimple.Value);
        }

        /// <summary>
        /// Updates the opacity of the simple/cartographic stroke
        /// </summary>
        /// <param name="value">THe floating point value to use for the opacity, where 0 is transparent and 1 is opaque</param>
        private void UpdateOpacity(float value)
        {
            if (_ignoreChanges) return;
            IStroke stroke = ccStrokes.SelectedStroke;
            if (stroke == null) return;
            ISimpleStroke ss = stroke as ISimpleStroke;
            if (ss != null)
            {
                if (ss.Opacity != value)
                {
                    ss.Opacity = value;
                    cbColorSimple.Color = ss.Color;
                    cbColorCartographic.Color = ss.Color;
                }
            }
        }

        private void dblStrokeOffset_TextChanged(object sender, EventArgs e)
        {
            if (ccStrokes == null) return;
            ICartographicStroke cs = ccStrokes.SelectedStroke as ICartographicStroke;
            if (cs == null) return;
            cs.Offset = (float)dblStrokeOffset.Value;
            UpdatePreview();
        }

        private void dblOffset_TextChanged(object sender, EventArgs e)
        {
            if (ccDecorations.SelectedDecoration == null) return;
            ccDecorations.SelectedDecoration.Offset = dblOffset.Value;
            UpdatePreview();
        }

        private void lblDecorationPreview_Paint(object sender, PaintEventArgs e)
        {
            UpdateDecorationPreview(e.Graphics);
        }

        private void ccDecorations_ListChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void ccDecorations_SelectedItemChanged(object sender, EventArgs e)
        {
            if (ccDecorations.SelectedDecoration != null)
            {
                UpdateDecorationControls();
            }
            UpdatePreview();
        }

        private void ccDecorations_AddClicked(object sender, EventArgs e)
        {
            ICartographicStroke currentStroke = ccStrokes.SelectedStroke as ICartographicStroke;
            if (currentStroke != null)
            {
                LineDecoration decoration = new LineDecoration();
                currentStroke.Decorations.Add(decoration);
                ccDecorations.RefreshList();
                ccDecorations.SelectedDecoration = decoration;
            }
        }

        private void dblWidthCartographic_TextChanged(object sender, EventArgs e)
        {
            UpdateWidth(dblWidthCartographic.Value);
        }

        private void LoadLineCaps()
        {
            Array names = Enum.GetValues(typeof(LineCap));
            foreach (object name in names)
            {
                cmbEndCap.Items.Add(name);
                cmbStartCap.Items.Add(name);
            }
        }

        private void dashControl1_PatternChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            IStroke stroke = ccStrokes.SelectedStroke;
            if (stroke == null) return;
            ICartographicStroke cs = stroke as ICartographicStroke;
            if (cs != null)
            {
                cs.DashStyle = DashStyle.Custom;
                cs.DashPattern = dashControl1.GetDashPattern();
                cs.DashButtons = dashControl1.DashButtons;
                cs.CompoundButtons = dashControl1.CompoundButtons;
                cs.CompoundArray = dashControl1.GetCompoundArray();
                cs.Width = cs.CompoundButtons.Length;
            }
            UpdatePreview();
        }

        private void ccStrokes_ListChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void lblPreview_Paint(object sender, PaintEventArgs e)
        {
            UpdatePreview(e.Graphics);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the control by updating the symbolizer
        /// </summary>
        /// <param name="original"></param>
        public void Initialize(ILineSymbolizer original)
        {
            _original = original;
            _symbolizer = original.Copy();
            ccStrokes.Strokes = _symbolizer.Strokes;
            chkSmoothing.Checked = _symbolizer.Smoothing;
            ccStrokes.RefreshList();
            if (_symbolizer.Strokes.Count > 0)
            {
                ccStrokes.SelectedStroke = _symbolizer.Strokes[0];
            }
            UpdatePreview();
            UpdateStrokeControls();
        }

        /// <summary>
        /// When the stroke is changed, this updates the controls to match it.
        /// </summary>
        private void UpdateStrokeControls()
        {
            _ignoreChanges = true;
            ICartographicStroke cs = ccStrokes.SelectedStroke as ICartographicStroke;
            if (cs != null)
            {
                cmbStrokeType.SelectedItem = "Cartographic";
                if (tabStrokeProperties.TabPages.Contains(tabSimple))
                {
                    tabStrokeProperties.TabPages.Remove(tabSimple);
                }
                if (tabStrokeProperties.TabPages.Contains(tabCartographic) == false)
                {
                    tabStrokeProperties.TabPages.Add(tabCartographic);
                }
                if (tabStrokeProperties.TabPages.Contains(tabDash) == false)
                {
                    tabStrokeProperties.TabPages.Add(tabDash);
                }
                if (tabStrokeProperties.TabPages.Contains(tabDecoration) == false)
                {
                    tabStrokeProperties.TabPages.Add(tabDecoration);
                }

                // Cartographic Tab Page
                if (cmbStartCap.Items.Count == 0)
                {
                    LoadLineCaps();
                }
                cmbStartCap.SelectedItem = cs.StartCap;
                cmbEndCap.SelectedItem = cs.EndCap;
                radLineJoin.Value = cs.JoinType;
                dblStrokeOffset.Value = cs.Offset;

                // Template Tab Page
                dashControl1.SetPattern(cs);
                dashControl1.Invalidate();

                // Decoration Tab Page
                ccDecorations.Decorations = cs.Decorations;
                if (cs.Decorations != null && cs.Decorations.Count > 0)
                {
                    ccDecorations.SelectedDecoration = cs.Decorations[0];
                }
            }
            else
            {
                cmbStrokeType.SelectedItem = "Simple";
                if (tabStrokeProperties.TabPages.Contains(tabDash))
                {
                    tabStrokeProperties.TabPages.Remove(tabDash);
                }
                if (tabStrokeProperties.TabPages.Contains(tabCartographic))
                {
                    tabStrokeProperties.TabPages.Remove(tabCartographic);
                }
                if (tabStrokeProperties.TabPages.Contains(tabDecoration))
                {
                    tabStrokeProperties.TabPages.Remove(tabDecoration);
                }
                if (tabStrokeProperties.TabPages.Contains(tabSimple) == false)
                {
                    tabStrokeProperties.TabPages.Add(tabSimple);
                }
            }
            ISimpleStroke ss = ccStrokes.SelectedStroke as ISimpleStroke;
            if (ss != null)
            {
                // Simple Tab Page
                cbColorSimple.Color = ss.Color;
                cbColorCartographic.Color = ss.Color;
                dblWidthCartographic.Value = ss.Width;
                dblWidth.Value = ss.Width;
                cmbStrokeStyle.SelectedIndex = (int)ss.DashStyle;
                sldOpacityCartographic.MaximumColor = Color.FromArgb(255, ss.Color.R, ss.Color.G, ss.Color.B);
                sldOpacitySimple.MaximumColor = Color.FromArgb(255, ss.Color.R, ss.Color.G, ss.Color.B);
                sldOpacitySimple.Value = ss.Opacity;
                sldOpacityCartographic.Value = ss.Opacity;
                sldOpacityCartographic.Invalidate();
                sldOpacitySimple.Invalidate();
            }

            _ignoreChanges = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current (copied) symbolizer or initializes this control to work with the
        /// specified symbolizer as the original.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILineSymbolizer Symbolizer
        {
            get { return _symbolizer; }
            set
            {
                if (value != null) Initialize(value);
            }
        }

        #endregion

        #region  Protected Methods

        /// <summary>
        /// Forces the original to apply the changes to the new control
        /// </summary>
        public void ApplyChanges()
        {
            OnApplyChanges();
        }
      

        /// <summary>
        /// Fires the AddtoCustomSymbols event
        /// </summary>
        protected virtual void OnAddToCustomSymbols()
        {
            AddToCustomSymbols(this, new LineSymbolizerEventArgs(_symbolizer));
        }

        /// <summary>
        /// Fires the ChangesApplied event
        /// </summary>
        protected void OnApplyChanges()
        {
            _original.CopyProperties(_symbolizer);
            if (ChangesApplied != null) ChangesApplied(this, EventArgs.Empty);
        }

        #endregion

        #region Event Handlers

        private void dblWidth_TextChanged(object sender, EventArgs e)
        {
            UpdateWidth(dblWidth.Value);
        }

        private void UpdateWidth(double value)
        {
            if (_ignoreChanges) return;
            IStroke stroke = ccStrokes.SelectedStroke;
            if (stroke == null) return;
            ISimpleStroke ss = stroke as ISimpleStroke;
            if (ss != null)
            {
                ss.Width = dblWidth.Value;
            }
            // only call if changed, or else we will create an infinite loop here
            if (dblWidth.Value != value) dblWidth.Value = value;
            if (dblWidthCartographic.Value != value) dblWidthCartographic.Value = value;
            UpdatePreview();
        }

        private void SetColor(Color color)
        {
            if (_ignoreChanges) return;
            IStroke stroke = ccStrokes.SelectedStroke;
            if (stroke == null) return;
            ISimpleStroke ss = stroke as ISimpleStroke;
            if (ss != null)
            {
                ss.Color = color;
            }
            ICartographicStroke cs = stroke as ICartographicStroke;
            if (cs != null)
            {
                dashControl1.LineColor = color;
            }
            // only call if changed, or we will create an infinite loop here
            if (cbColorSimple.Color != color) cbColorSimple.Color = color;
            if (cbColorCartographic.Color != color) cbColorCartographic.Color = color;
            sldOpacityCartographic.MaximumColor = Color.FromArgb(255, color);
            sldOpacitySimple.MaximumColor = Color.FromArgb(255, color);
            sldOpacityCartographic.Invalidate();
            sldOpacitySimple.Invalidate();
            UpdatePreview();
        }

        private void ccStrokes_AddClicked(object sender, EventArgs e)
        {
            if (cmbStrokeType.SelectedItem.ToString() == "Simple")
            {
                _symbolizer.Strokes.Add(new SimpleStroke());
                ccStrokes.RefreshList();
            }
            if (cmbStrokeType.SelectedItem.ToString() == "Cartographic")
            {
                _symbolizer.Strokes.Add(new CartographicStroke());
                ccStrokes.RefreshList();
            }
            UpdatePreview();
        }

        private void ccStrokes_SelectedItemChanged(object sender, EventArgs e)
        {
            if (ccStrokes.SelectedStroke != null)
            {
                UpdateStrokeControls();
            }
            UpdatePreview();
        }

        private void UpdateDecorationControls()
        {
            _ignoreChanges = true;
            ILineDecoration decoration = ccDecorations.SelectedDecoration;
            if (decoration != null)
            {
                chkFlipAll.Checked = decoration.FlipAll;
                chkFlipFirst.Checked = decoration.FlipFirst;
                if (decoration.RotateWithLine)
                {
                    radRotationWithLine.Checked = true;
                }
                else
                {
                    radRotationFixed.Checked = true;
                }
                nudDecorationCount.Value = decoration.NumSymbols;
                nudPercentualPosition.Value = decoration.PercentualPosition;
                nudPercentualPosition.Visible = decoration.NumSymbols == 1;
                lblPercentualPosition.Visible = decoration.NumSymbols == 1;
                dblOffset.Value = decoration.Offset;
            }
            _ignoreChanges = false;
        }

        private void cmbStrokeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            int index = ccStrokes.Strokes.IndexOf(ccStrokes.SelectedStroke);
            if (index == -1) return;
            string strokeType = cmbStrokeType.SelectedItem.ToString();
            if (strokeType == "Cartographic")
            {
                ICartographicStroke cs = new CartographicStroke();
                ccStrokes.Strokes[index] = cs;
                ccStrokes.RefreshList();
                ccStrokes.SelectedStroke = cs;
                //UpdateStrokeControls();
            }
            else if (strokeType == "Simple")
            {
                ISimpleStroke ss = new SimpleStroke();
                ccStrokes.Strokes[index] = ss;
                ccStrokes.RefreshList();
                ccStrokes.SelectedStroke = ss;
                // UpdateStrokeControls();
            }
            //UpdatePreview();
        }

        private void cmbScaleMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbScaleMode.SelectedItem.ToString())
            {
                case "Simple": _symbolizer.ScaleMode = ScaleMode.Simple; break;
                case "Geographic": _symbolizer.ScaleMode = ScaleMode.Geographic; break;
                case "Symbolic": _symbolizer.ScaleMode = ScaleMode.Symbolic; break;
            }
        }

        private void chkSmoothing_CheckedChanged(object sender, EventArgs e)
        {
            _symbolizer.Smoothing = chkSmoothing.Checked;
            UpdatePreview();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            if (ccStrokes.SelectedStroke == null) return;
            ISimpleStroke ss = ccStrokes.SelectedStroke as ISimpleStroke;
            if (ss != null) ss.DashStyle = (DashStyle)cmbStrokeStyle.SelectedIndex;
            UpdatePreview();
        }

        private void cmbStartCap_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            if (ccStrokes.SelectedStroke == null) return;
            IStroke stroke = ccStrokes.SelectedStroke;
            ICartographicStroke cs = stroke as ICartographicStroke;
            if (cs != null && cmbStartCap.SelectedIndex != -1)
            {
                cs.StartCap = (LineCap)cmbStartCap.SelectedItem;
            }
            UpdatePreview();
        }

        private void cmbEndCap_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            if (ccStrokes.SelectedStroke == null) return;
            IStroke stroke = ccStrokes.SelectedStroke;
            ICartographicStroke cs = stroke as ICartographicStroke;
            if (cs != null && cmbEndCap.SelectedIndex != -1)
            {
                cs.EndCap = (LineCap)cmbEndCap.SelectedItem;
            }
            UpdatePreview();
        }

        private void nudDecorationCount_ValueChanged(object sender, EventArgs e)
        {
            nudPercentualPosition.Visible = nudDecorationCount.Value == 1;
            lblPercentualPosition.Visible = nudDecorationCount.Value == 1;
            if (_ignoreChanges) return;
            ILineDecoration dec = ccDecorations.SelectedDecoration;
            if (dec != null)
            {
                dec.NumSymbols = (int)nudDecorationCount.Value;
            }
            UpdatePreview();
        }

        /// <summary>
        /// Update the selected linedecoration: set the percentual position between line start and end of the single decoration.
        /// </summary>
        private void nudPercentualPosition_ValueChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            ILineDecoration dec = ccDecorations.SelectedDecoration;
            if (dec != null)
            {
                dec.PercentualPosition = (int)nudPercentualPosition.Value;
            }
            UpdatePreview();
        }

        /// <summary>
        /// Update the selected linedecoration: set whether all decorations should be flipped.
        /// </summary>
        private void chkFlipAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            ILineDecoration dec = ccDecorations.SelectedDecoration;
            if (dec != null)
            {
                dec.FlipAll = chkFlipAll.Checked;
            }
            UpdatePreview();
        }

        /// <summary>
        /// Update the selected linedecoration: set whether the first decoration should be flipped.
        /// </summary>
        private void chkFlipFirst_CheckedChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            ILineDecoration dec = ccDecorations.SelectedDecoration;
            if (dec != null)
            {
                dec.FlipFirst = chkFlipFirst.Checked;
            }
            UpdatePreview();
        }

        private void radRotationWithLine_CheckedChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;
            ILineDecoration dec = ccDecorations.SelectedDecoration;
            if (dec != null)
            {
                dec.RotateWithLine = radRotationWithLine.Checked;
            }
            UpdatePreview();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            ILineDecoration dec = ccDecorations.SelectedDecoration;
            if (dec == null) return;
            DetailedPointSymbolDialog dpd = new DetailedPointSymbolDialog(dec.Symbol);
            dpd.ChangesApplied += dpd_ChangesApplied;
            dpd.ShowDialog();
        }

        private void dpd_ChangesApplied(object sender, EventArgs e)
        {
            UpdatePreview();
            ccDecorations.Refresh();
            lblDecorationPreview.Refresh();
        }

        private void btnAddToCustom_Click(object sender, EventArgs e)
        {
            OnAddToCustomSymbols();
        }

        #endregion

        #region Private Functions

        private void UpdatePreview()
        {
            Graphics g = lblPreview.CreateGraphics();
            UpdatePreview(g);
            g.Dispose();

            ICartographicStroke cs = ccStrokes.SelectedStroke as ICartographicStroke;
            if (cs != null)
            {
                g = lblDecorationPreview.CreateGraphics();
                UpdateDecorationPreview(g);
                g.Dispose();
            }
        }

        private void UpdatePreview(Graphics g)
        {
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, lblPreview.Width, lblPreview.Height));
            GraphicsPath gp = new GraphicsPath();
            gp.AddLine(10, lblPreview.Height / 2, lblPreview.Width - 20, lblPreview.Height / 2);
            foreach (IStroke stroke in _symbolizer.Strokes)
            {
                stroke.DrawPath(g, gp, 1);
            }
            gp.Dispose();
            ccStrokes.Refresh();
        }

        private void UpdateDecorationPreview(Graphics g)
        {
            ILineDecoration dec = ccDecorations.SelectedDecoration;
            if (dec != null)
            {
                dec.Symbol.Draw(g, lblDecorationPreview.ClientRectangle);
            }
        }

        #endregion
    }
}