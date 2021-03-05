using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    partial class CirSim {
        public CirSim() {
            theSim = this;
            mCir = new Circuit(this);
            Menu = new MenuItems(this);
        }

        public void init(Form parent) {
            mParent = parent;

            mParent.KeyPreview = true;
            mParent.KeyDown += onKeyDown;
            mParent.KeyUp += onKeyUp;

            elmList = new List<CircuitElm>();
            redoItem = new MenuItem();
            undoItem = new MenuItem();
            pasteItem = new MenuItem();
            transform = new float[] { 1, 0, 0, 1, 0, 0 };

            scopes = new Scope[20];
            scopeColCount = new int[20];
            scopeCount = 0;

            setTimer();

            verticalPanel = new Panel();
            {
                int ofsY = 0;
                /* Reset */
                resetButton = new Button() { AutoSize = true, Text = "Reset" };
                resetButton.Click += new EventHandler((s, e) => { resetButton_onClick(); });
                resetButton.Left = 4;
                resetButton.Top = ofsY;
                verticalPanel.Controls.Add(resetButton);
                ofsY += resetButton.Height + 4;

                /* Run */
                runStopButton = new Button() { AutoSize = true, Text = "RUN" };
                runStopButton.Click += new EventHandler((s, e) => { SetSimRunning(!SimIsRunning()); });
                runStopButton.Left = 4;
                runStopButton.Top = ofsY;
                verticalPanel.Controls.Add(runStopButton);
                ofsY += runStopButton.Height + 4;

                /* Simulation Speed */
                var lbl = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "Simulation Speed" };
                verticalPanel.Controls.Add(lbl);
                ofsY += lbl.Height;
                trbSpeedBar = new TrackBar() {
                    Left = 4,
                    Top = ofsY,
                    Minimum = 0,
                    Maximum = 1000,
                    SmallChange = 1,
                    LargeChange = 10,
                    TickFrequency = 100,
                    TickStyle = TickStyle.TopLeft,
                    Value = 10,
                    Width = 200
                };
                verticalPanel.Controls.Add(trbSpeedBar);
                ofsY += trbSpeedBar.Height + 4;

                /* Current Speed */
                lbl = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "Current Speed" };
                verticalPanel.Controls.Add(lbl);
                ofsY += lbl.Height;
                trbCurrentBar = new TrackBar() {
                    Left = 4,
                    Top = ofsY,
                    Minimum = 1,
                    Maximum = 100,
                    SmallChange = 1,
                    LargeChange = 10,
                    TickFrequency = 10,
                    TickStyle = TickStyle.TopLeft,
                    Value = 50,
                    Width = 200
                };
                verticalPanel.Controls.Add(trbCurrentBar);
                ofsY += trbCurrentBar.Height + 4;

                /* Show Voltage */
                chkShowVolts = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "電圧を表示" };
                verticalPanel.Controls.Add(chkShowVolts);
                ofsY += chkShowVolts.Height + 4;

                /* Show Current */
                chkShowDots = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "電流を表示" };
                verticalPanel.Controls.Add(chkShowDots);
                ofsY += chkShowDots.Height + 4;

                /* Show Values */
                chkShowValues = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "値を表示" };
                verticalPanel.Controls.Add(chkShowValues);
                ofsY += chkShowValues.Height + 4;

                /* ANSI */
                chkUseAnsiSymbols = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "ANSI" };
                chkUseAnsiSymbols.CheckedChanged += new EventHandler((s, e) => {
                    setOptionInStorage("ansiResistors", chkUseAnsiSymbols.Checked);
                });
                verticalPanel.Controls.Add(chkUseAnsiSymbols);
                ofsY += chkUseAnsiSymbols.Height + 4;

                /* White Background */
                chkPrintable = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "白黒表示" };
                chkPrintable.CheckedChanged += new EventHandler((s, e) => {
                    for (int i = 0; i < scopeCount; i++) {
                        scopes[i].SetRect(scopes[i].BoundingBox);
                    }
                    setOptionInStorage("whiteBackground", chkPrintable.Checked);
                });
                verticalPanel.Controls.Add(chkPrintable);
                ofsY += chkPrintable.Height + 4;

                /* Show Cursor Cross Hairs */
                chkCrossHair = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "ポインターを表示" };
                chkCrossHair.CheckedChanged += new EventHandler((s, e) => {
                    setOptionInStorage("crossHair", chkCrossHair.Checked);
                });
                verticalPanel.Controls.Add(chkCrossHair);
                ofsY += chkCrossHair.Height + 4;

                /* iFrame */
                iFrame = new Panel() {
                    Left = 4,
                    Top = ofsY,
                    BorderStyle = BorderStyle.Fixed3D,
                    AutoScroll = true
                };
                verticalPanel.Controls.Add(iFrame);

                /* */
                verticalPanel.Width = trbSpeedBar.Width + 12;
                verticalPanel.Height = ofsY;
            }

            CircuitElm.InitClass(this, mCir);
            readRecovery();

            menuBar = new MenuStrip();
            {
                Menu.composeMainMenu(menuBar);
                parent.Controls.Add(menuBar);
            }

            verticalPanel.Top = menuBar.Height;
            verticalPanel.Height += menuBar.Height;

            picCir = new PictureBox() { Left = 0, Top = menuBar.Height };
            {
                picCir.MouseDown += new MouseEventHandler((s, e) => { onMouseDown(e); });
                picCir.MouseMove += new MouseEventHandler((s, e) => { onMouseMove(e); });
                picCir.MouseUp += new MouseEventHandler((s, e) => { onMouseUp(e); });
                picCir.MouseWheel += new MouseEventHandler((s, e) => { onMouseWheel((PictureBox)s, e); });
                picCir.MouseClick += new MouseEventHandler((s, e) => { onClick((PictureBox)s, e); });
                picCir.MouseLeave += new EventHandler((s, e) => { onMouseLeave(); });
                picCir.DoubleClick += new EventHandler((s, e) => { onDoubleClick(e); });
            }

            layoutPanel = new SplitContainer();
            {
                layoutPanel.Dock = DockStyle.Fill;
                layoutPanel.BorderStyle = BorderStyle.FixedSingle;
                layoutPanel.IsSplitterFixed = true;
                layoutPanel.Panel1.Controls.Add(picCir);
                layoutPanel.Panel2.Controls.Add(verticalPanel);
                int width = verticalPanel.Width;
                layoutPanel.SizeChanged += new EventHandler((s, e) => {
                    if (0 <= layoutPanel.Width - width) {
                        layoutPanel.SplitterDistance = layoutPanel.Width - width;
                        setCanvasSize();
                    }
                });
                parent.Controls.Add(layoutPanel);
            }

            var ctxMenuItem = new List<ToolStripMenuItem>();
            {
                ctxMenuItem.Add(elmEditMenuItem = new ToolStripMenuItem() { Text = "Edit..." });
                elmEditMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.edit);
                });
                ctxMenuItem.Add(elmScopeMenuItem = new ToolStripMenuItem() { Text = "View in Scope" });
                elmScopeMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.VIEW_IN_SCOPE);
                });
                ctxMenuItem.Add(elmFloatScopeMenuItem = new ToolStripMenuItem() { Text = "View in Undocked Scope" });
                elmFloatScopeMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.VIEW_IN_FLOAT_SCOPE);
                });
                ctxMenuItem.Add(elmCutMenuItem = new ToolStripMenuItem() { Text = "Cut" });
                elmCutMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.CUT);
                });
                ctxMenuItem.Add(elmCopyMenuItem = new ToolStripMenuItem() { Text = "Copy" });
                elmCopyMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.COPY);
                });
                ctxMenuItem.Add(elmDeleteMenuItem = new ToolStripMenuItem() { Text = "Delete" });
                elmDeleteMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.DELETE);
                });
                var dup = new ToolStripMenuItem() { Text = "Duplicate" };
                ctxMenuItem.Add(dup);
                dup.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.DUPLICATE);
                });
                ctxMenuItem.Add(elmFlipMenuItem = new ToolStripMenuItem() { Text = "Swap Terminals" });
                elmFlipMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.flip);
                });
                ctxMenuItem.Add(elmSplitMenuItem = new ToolStripMenuItem() { Text = "Split Wire" });
                elmSplitMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.split);
                });
                ctxMenuItem.Add(elmSliderMenuItem = new ToolStripMenuItem() { Text = "Sliders..." });
                elmSliderMenuItem.Click += new EventHandler((s, e) => {
                    MenuPerformed(MENU_CATEGORY.ELEMENTS, MENU_ITEM.sliders);
                });
                /* */
                elmMenuBar = ctxMenuItem.ToArray();
            }

            setGrid();

            CircuitElm.SetColorScale(64);

            if (startCircuitText != null) {
                getSetupList(false);
                readCircuit(startCircuitText);
                unsavedChanges = false;
            } else {
                if (mCir.StopMessage == null && startCircuitLink != null) {
                    readCircuit("");
                    getSetupList(false);
                } else {
                    readCircuit("");
                    if (mCir.StopMessage == null && startCircuit != null) {
                        getSetupList(false);
                        readSetupFile(startCircuit, startLabel);
                    } else {
                        getSetupList(true);
                    }
                }
            }

            enableUndoRedo();
            enablePaste();
            SetiFrameHeight();

            scopePopupMenu = new ScopePopupMenu(this);

            SetSimRunning(true);
        }

        #region Public method
        public void SetiFrameHeight() {
            if (iFrame == null) {
                return;
            }
            int height = 0;
            for (int i = 0; i < iFrame.Controls.Count; i++) {
                if (height < iFrame.Controls[i].Bottom) {
                    height = iFrame.Controls[i].Bottom;
                }
            }
            if (mParent.Height < iFrame.Top + height + 55) {
                height = mParent.Height - iFrame.Top - 55;
            }
            iFrame.Height = height + 4;
            verticalPanel.Height = iFrame.Bottom + 4;
        }

        public void SetSimRunning(bool s) {
            if (s) {
                if (mCir.StopMessage != null) {
                    return;
                }
                simRunning = true;
                runStopButton.Text = "RUN";
            } else {
                simRunning = false;
                runStopButton.Text = "STOP";
                Repaint();
            }
        }

        public bool SimIsRunning() {
            return simRunning;
        }

        public void Repaint() {
            if (!needsRepaint) {
                needsRepaint = true;
                updateCircuit();
                needsRepaint = false;
            }
        }

        public Color getBackgroundColor() {
            if (chkPrintable.Checked) {
                return Color.White;
            }
            return Color.Black;
        }

        public void NeedAnalyze() {
            analyzeFlag = true;
            Repaint();
        }

        public Adjustable FindAdjustable(CircuitElm elm, int item) {
            for (int i = 0; i != adjustables.Count; i++) {
                var a = adjustables[i];
                if (a.elm == elm && a.editItem == item) {
                    return a;
                }
            }
            return null;
        }

        public void MenuPerformed(MENU_CATEGORY cat, MENU_ITEM item, string option = "") {
            if (item == MENU_ITEM.OPEN_FILE) {
                doOpenFile();
            }
            if (item == MENU_ITEM.SAVE_FILE) {
                doSaveFile();
                unsavedChanges = false;
            }
            if (item == MENU_ITEM.createsubcircuit) {
                doCreateSubcircuit();
            }
            if (item == MENU_ITEM.dcanalysis) {
                doDCAnalysis();
            }
            if (item == MENU_ITEM.print) {
                backContext.Print();
            }
            if (item == MENU_ITEM.recover) {
                doRecover();
            }

            if ((cat == MENU_CATEGORY.ELEMENTS || cat == MENU_CATEGORY.SCOPE_POP) && contextPanel != null) {
                contextPanel.Close();
            }

            if (cat == MENU_CATEGORY.OPTIONS && item == MENU_ITEM.OTHER) {
                doEdit(new EditOptions(this));
            }

            if (cat == MENU_CATEGORY.KEY && mouseElm != null) {
                menuElm = mouseElm;
                cat = MENU_CATEGORY.ELEMENTS;
            }

            if (item == MENU_ITEM.UNDO) {
                doUndo();
            }
            if (item == MENU_ITEM.REDO) {
                doRedo();
            }
            if (item == MENU_ITEM.CUT) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    menuElm = null;
                }
                doCut();
            }
            if (item == MENU_ITEM.COPY) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    menuElm = null;
                }
                doCopy();
            }
            if (item == MENU_ITEM.PASTE) {
                doPaste(null);
            }
            if (item == MENU_ITEM.DELETE) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    menuElm = null;
                }
                pushUndo();
                doDelete(true);
            }
            if (item == MENU_ITEM.DUPLICATE) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    menuElm = null;
                }
                doDuplicate();
            }
            if (item == MENU_ITEM.SELECT_ALL) {
                doSelectAll();
            }

            if (item == MENU_ITEM.ZOOM_IN) {
                zoomCircuit(20);
            }
            if (item == MENU_ITEM.ZOOM_OUT) {
                zoomCircuit(-20);
            }
            if (item == MENU_ITEM.ZOOM_100) {
                setCircuitScale(1);
            }
            if (item == MENU_ITEM.CENTER_CIRCUIT) {
                pushUndo();
                centreCircuit();
            }

            if (item == MENU_ITEM.flip) {
                doFlip();
            }
            if (item == MENU_ITEM.split) {
                doSplit(menuElm);
            }

            if (item == MENU_ITEM.STACK_ALL) {
                stackAll();
            }
            if (item == MENU_ITEM.UNSTACK_ALL) {
                unstackAll();
            }
            if (item == MENU_ITEM.COMBINE_ALL) {
                combineAll();
            }
            if (item == MENU_ITEM.SEPARATE_ALL) {
                separateAll();
            }

            if (cat == MENU_CATEGORY.ELEMENTS && item == MENU_ITEM.edit) {
                doEdit(menuElm);
            }
            if (item == MENU_ITEM.sliders) {
                doSliders(menuElm);
            }

            if (item == MENU_ITEM.VIEW_IN_SCOPE && menuElm != null) {
                int i;
                for (i = 0; i != scopeCount; i++) {
                    if (scopes[i].Elm == null) {
                        break;
                    }
                }
                if (i == scopeCount) {
                    if (scopeCount == scopes.Length) {
                        return;
                    }
                    scopeCount++;
                    scopes[i] = new Scope(this);
                    scopes[i].Position = i;
                }
                scopes[i].SetElm(menuElm);
                if (i > 0) {
                    scopes[i].Speed = scopes[i - 1].Speed;
                }
            }

            if (item == MENU_ITEM.VIEW_IN_FLOAT_SCOPE && menuElm != null) {
                var newScope = new ScopeElm(snapGrid(menuElm.X1 + 50), snapGrid(menuElm.Y1 + 50));
                elmList.Add(newScope);
                newScope.setScopeElm(menuElm);
            }

            if (cat == MENU_CATEGORY.SCOPE_POP) {
                pushUndo();
                Scope s;
                if (menuScope != -1) {
                    s = scopes[menuScope];
                } else {
                    if (mouseElm is ScopeElm) {
                        s = ((ScopeElm)mouseElm).elmScope;
                    } else {
                        return;
                    }
                }

                if (item == MENU_ITEM.dock) {
                    if (scopeCount == scopes.Length) {
                        return;
                    }
                    scopes[scopeCount] = ((ScopeElm)mouseElm).elmScope;
                    ((ScopeElm)mouseElm).clearElmScope();
                    scopes[scopeCount].Position = scopeCount;
                    scopeCount++;
                    doDelete(false);
                }
                if (item == MENU_ITEM.undock && 0 <= menuScope) {
                    var newScope = new ScopeElm(snapGrid(menuElm.X1 + 50), snapGrid(menuElm.Y1 + 50));
                    elmList.Add(newScope);
                    newScope.setElmScope(scopes[menuScope]);
                    /* remove scope from list.  setupScopes() will fix the positions */
                    for (int i = menuScope; i < scopeCount; i++) {
                        scopes[i] = scopes[i + 1];
                    }
                    scopeCount--;
                }
                if (null != s) {
                    if (item == MENU_ITEM.remove) {
                        s.SetElm(null);  /* setupScopes() will clean this up */
                    }
                    if (item == MENU_ITEM.removeplot) {
                        s.RemovePlot(menuPlot);
                    }
                    if (item == MENU_ITEM.speed2) {
                        s.SpeedUp();
                    }
                    if (item == MENU_ITEM.speed1_2) {
                        s.SlowDown();
                    }
                    if (item == MENU_ITEM.maxscale) {
                        s.MaxScale();
                    }
                    if (item == MENU_ITEM.stack) {
                        stackScope(menuScope);
                    }
                    if (item == MENU_ITEM.unstack) {
                        unstackScope(menuScope);
                    }
                    if (item == MENU_ITEM.combine) {
                        combineScope(menuScope);
                    }
                    if (item == MENU_ITEM.selecty) {
                        s.SelectY();
                    }
                    if (item == MENU_ITEM.reset) {
                        s.ResetGraph(true);
                    }
                    if (item == MENU_ITEM.properties) {
                        s.Properties(mParent.Location.X, mParent.Location.Y);
                    }
                }
                deleteUnusedScopeElms();
            }

            if (cat == MENU_CATEGORY.CIRCUITS && item == MENU_ITEM.SETUP) {
                pushUndo();
                if (!string.IsNullOrEmpty(option)) {
                    int sp = option.IndexOf(' ');
                    readSetupFile(option.Substring(0, sp), option.Substring(sp + 1));
                }
            }

            /* IES: Moved from itemStateChanged() */
            if (cat == MENU_CATEGORY.MAIN) {
                if (contextPanel != null) {
                    contextPanel.Close();
                }
                setMouseMode(MOUSE_MODE.ADD_ELM);
                if (item != MENU_ITEM.INVALID) {
                    mouseModeStr = item;
                }
                if (item == MENU_ITEM.SELECT) {
                    setMouseMode(MOUSE_MODE.SELECT);
                }
                tempMouseMode = mouseMode;
            }
            Repaint();
        }

        /* delete sliders for an element */
        public void DeleteSliders(CircuitElm elm) {
            if (adjustables == null) {
                return;
            }
            for (int i = adjustables.Count - 1; i >= 0; i--) {
                var adj = adjustables[i];
                if (adj.elm == elm) {
                    adj.deleteSlider(this);
                    adjustables.RemoveAt(i);
                }
            }
        }

        public int LocateElm(CircuitElm elm) {
            for (int i = 0; i != elmList.Count; i++) {
                if (elm == elmList[i]) {
                    return i;
                }
            }
            return -1;
        }

        public void AddWidgetToVerticalPanel(Control ctrl) {
            if (iFrame == null) {
                return;
            }
            var ofsY = 4;
            for (int i = 0; i < iFrame.Controls.Count; i++) {
                if (ofsY < iFrame.Controls[i].Bottom) {
                    ofsY = iFrame.Controls[i].Bottom;
                }
            }
            ctrl.Top = ofsY;
            iFrame.Controls.Add(ctrl);
            SetiFrameHeight();
        }

        public void RemoveWidgetFromVerticalPanel(Control ctrl) {
            if (iFrame == null) {
                return;
            }
            int ofsY = 4;
            iFrame.SuspendLayout();
            iFrame.Controls.Remove(ctrl);
            for (int i = 0; i < iFrame.Controls.Count; i++) {
                iFrame.Controls[i].Top = ofsY;
                ofsY += iFrame.Controls[i].Height + 4;
            }
            iFrame.ResumeLayout(false);
            SetiFrameHeight();
        }

        public bool DialogIsShowing() {
            if (editDialog != null && editDialog.Visible) {
                return true;
            }
            if (sliderDialog != null && sliderDialog.Visible) {
                return true;
            }
            if (customLogicEditDialog != null && customLogicEditDialog.Visible) {
                return true;
            }
            if (diodeModelEditDialog != null && diodeModelEditDialog.Visible) {
                return true;
            }
            if (dialogShowing != null && dialogShowing.Visible) {
                return true;
            }
            if (contextPanel != null && contextPanel.Visible) {
                return true;
            }
            if (scrollValuePopup != null && scrollValuePopup.Visible) {
                return true;
            }
            // TODO: dialogIsShowing
            //if (aboutBox != null && aboutBox.isShowing())
            //    return true;
            return false;
        }

        public void UpdateModels() {
            for (int i = 0; i != elmList.Count; i++) {
                elmList[i].UpdateModels();
            }
        }

        /* convert grid coordinates to screen coordinates */
        public int TransformX(double x) {
            return (int)((x * transform[0]) + transform[4]);
        }
        public int TransformY(double y) {
            return (int)((y * transform[3]) + transform[5]);
        }
        #endregion

        void onKeyDown(object sender, KeyEventArgs e) {
            isPressShift = e.Shift;
            isPressCtrl = e.Control;
            isPressAlt = e.Alt;
        }

        void onKeyUp(object sender, KeyEventArgs e) {
            isPressShift = false;
            isPressCtrl = false;
            isPressAlt = false;
            setCursorStyle(Cursors.Arrow);
            onPreviewNativeEvent(e);
        }

        void onClick(Control s, MouseEventArgs e) {
            if (e.Button == MouseButtons.Middle) {
                scrollValues(0);
            }
            if (e.Button == MouseButtons.Right) {
                onContextMenu(s, e);
            }
        }

        void onDoubleClick(EventArgs e) {
            if (mouseElm != null && !(mouseElm is SwitchElm)) {
                doEdit(mouseElm);
            }
        }

        void onMouseDown(MouseEventArgs e) {
            mCir.StopElm = null; /* if stopped, allow user to select other elements to fix circuit */
            menuX = menuClientX = mouseCursorX = e.X;
            menuY = menuClientY = mouseCursorY = e.Y;
            mouseButton = e.Button;
            mouseDownTime = DateTime.Now.ToFileTimeUtc();

            /* maybe someone did copy in another window?  should really do this when */
            /* window receives focus */
            enablePaste();

            if (mouseButton != MouseButtons.Left && mouseButton != MouseButtons.Middle) {
                return;
            }

            // set mouseElm in case we are on mobile
            mouseSelect();

            mouseDragging = true;
            didSwitch = false;

            if (mouseWasOverSplitter) {
                tempMouseMode = MOUSE_MODE.DRAG_SPLITTER;
                return;
            }
            if (mouseButton == MouseButtons.Left) {
                /* left mouse */
                tempMouseMode = mouseMode;
                if (isPressCtrl && isPressShift) {
                    tempMouseMode = MOUSE_MODE.DRAG_COLUMN;
                    setCursorStyle(Cursors.SizeWE);
                } else if (isPressCtrl && isPressAlt) {
                    tempMouseMode = MOUSE_MODE.DRAG_ROW;
                    setCursorStyle(Cursors.SizeNS);
                } else if (isPressCtrl) {
                    tempMouseMode = MOUSE_MODE.SELECT;
                    setCursorStyle(Cursors.Arrow);
                } else if (isPressShift) {
                    tempMouseMode = MOUSE_MODE.DRAG_POST;
                    setCursorStyle(Cursors.SizeAll);
                } else if (isPressAlt) {
                    tempMouseMode = MOUSE_MODE.DRAG_ALL;
                    setCursorStyle(Cursors.NoMove2D);
                }
            } else {
                tempMouseMode = MOUSE_MODE.DRAG_ALL;
            }

            if ((scopeSelected != -1 && scopes[scopeSelected].CursorInSettingsWheel) ||
                (scopeSelected == -1 && mouseElm != null && (mouseElm is ScopeElm) && ((ScopeElm)mouseElm).elmScope.CursorInSettingsWheel)) {
                Console.WriteLine("Doing something");
                Scope s;
                if (scopeSelected != -1) {
                    s = scopes[scopeSelected];
                } else {
                    s = ((ScopeElm)mouseElm).elmScope;
                }
                s.Properties(
                    mParent.Location.X + mouseCursorX,
                    mParent.Location.Y + mouseCursorY
                );
                clearSelection();
                mouseDragging = false;
                return;
            }

            int gx = inverseTransformX(e.X);
            int gy = inverseTransformY(e.Y);
            if (doSwitch(gx, gy)) {
                /* do this BEFORE we change the mouse mode to MODE_DRAG_POST!  Or else logic inputs */
                /* will add dots to the whole circuit when we click on them! */
                didSwitch = true;
                return;
            }

            /* IES - Grab resize handles in select mode if they are far enough apart and you are on top of them */
            if (tempMouseMode == MOUSE_MODE.SELECT && mouseElm != null
                && mouseElm.GetHandleGrabbedClose(gx, gy, POSTGRABSQ, MINPOSTGRABSIZE) >= 0
                && !anySelectedButMouse()) {
                tempMouseMode = MOUSE_MODE.DRAG_POST;
            }

            if (tempMouseMode != MOUSE_MODE.SELECT && tempMouseMode != MOUSE_MODE.DRAG_SELECTED) {
                clearSelection();
            }

            pushUndo();
            initDragGridX = gx;
            initDragGridY = gy;
            dragging = true;
            if (tempMouseMode != MOUSE_MODE.ADD_ELM) {
                return;
            }
            /* */
            int x0 = snapGrid(gx);
            int y0 = snapGrid(gy);
            if (!circuitArea.Contains(mouseCursorX, mouseCursorY)) {
                return;
            }

            dragElm = MenuItems.constructElement(mouseModeStr, x0, y0);
        }

        void onMouseUp(MouseEventArgs e) {
            mouseDragging = false;
            mouseButton = MouseButtons.None;

            /* click to clear selection */
            if (tempMouseMode == MOUSE_MODE.SELECT && selectedArea.Width == 0) {
                clearSelection();
            }

            /* cmd-click = split wire */
            if (tempMouseMode == MOUSE_MODE.DRAG_POST && draggingPost == -1) {
                doSplit(mouseElm);
            }

            tempMouseMode = mouseMode;
            selectedArea = new Rectangle();
            dragging = false;
            bool circuitChanged = false;
            if (heldSwitchElm != null) {
                heldSwitchElm.mouseUp();
                heldSwitchElm = null;
                circuitChanged = true;
            }
            if (dragElm != null) {
                /* if the element is zero size then don't create it */
                /* IES - and disable any previous selection */
                if (dragElm.IsCreationFailed) {
                    dragElm.Delete();
                    if (mouseMode == MOUSE_MODE.SELECT || mouseMode == MOUSE_MODE.DRAG_SELECTED) {
                        clearSelection();
                    }
                } else {
                    elmList.Add(dragElm);
                    dragElm.DraggingDone();
                    circuitChanged = true;
                    writeRecoveryToStorage();
                    unsavedChanges = true;
                }
                dragElm = null;
            }
            if (circuitChanged) {
                NeedAnalyze();
            }
            if (dragElm != null) {
                dragElm.Delete();
            }
            dragElm = null;
            Repaint();
        }

        void onMouseWheel(Control sender, MouseEventArgs e) {
            /* once we start zooming, don't allow other uses of mouse wheel for a while */
            /* so we don't accidentally edit a resistor value while zooming */
            bool zoomOnly = DateTime.Now.ToFileTimeUtc() < zoomTime + 1000;

            if (!zoomOnly) {
                scrollValues(e.Delta);
            }
            // TODO: onMouseWheel
            //if ((mouseElm is MouseWheelHandler) && !zoomOnly) {
            //    ((MouseWheelHandler)mouseElm).onMouseWheel(e);
            //}
            if (scopeSelected != -1) {
                scopes[scopeSelected].onMouseWheel(e);
            } else if (!DialogIsShowing()) {
                zoomCircuit(-e.Delta);
                zoomTime = DateTime.Now.ToFileTimeUtc();
            }
            Repaint();
        }

        void onMouseMove(MouseEventArgs e) {
            mouseCursorX = e.X;
            mouseCursorY = e.Y;
            if (33 < (DateTime.Now - mLastMouseMove).Milliseconds) {
                mLastMouseMove = DateTime.Now;
            } else {
                return;
            }
            if (mouseDragging) {
                mouseDragged();
                return;
            }
            mouseSelect();
        }

        void onMouseLeave() {
            mouseCursorX = -1;
            mouseCursorY = -1;
        }

        void resetButton_onClick() {
            for (int i = 0; i != elmList.Count; i++) {
                getElm(i).Reset();
            }
            for (int i = 0; i != scopeCount; i++) {
                scopes[i].ResetGraph(true);
            }
            analyzeFlag = true;
            if (t == 0) {
                SetSimRunning(true);
            } else {
                t = 0;
            }
        }

        void setTimer() {
            timer = new Timer();
            timer.Tick += new EventHandler((s, e) => {
                if (simRunning) {
                    updateCircuit();
                    needsRepaint = false;
                }
            });
            timer.Interval = 1;
            timer.Enabled = true;
            timer.Start();
        }

        void setCanvasSize() {
            int width = layoutPanel.Panel1.Width;
            int height = layoutPanel.Panel1.Height - menuBar.Height;
            if (width < 1) {
                width = 1;
            }
            if (height < 1) {
                height = 1;
            }
            var isRunning = SimIsRunning();
            if (isRunning) {
                SetSimRunning(false);
            }

            picCir.Width = width;
            picCir.Height = height;
            if (backContext != null) {
                backContext.Dispose();
            }
            backContext = CustomGraphics.FromImage(width, height);
            setCircuitArea();
            SetSimRunning(isRunning);
        }

        void setCircuitArea() {
            int height = picCir.Height;
            int width = picCir.Width;
            int h = (int)(height * scopeHeightFraction);
            circuitArea = new Rectangle(0, 0, width, height - h);
        }

        void setOptionInStorage(string key, bool val) {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            stor.setItem(key, val ? "true" : "false");
        }

        void centreCircuit() {
            var bounds = getCircuitBounds();

            double scale = 1;

            if (0 < bounds.Width) {
                /* add some space on edges because bounds calculation is not perfect */
                scale = Math.Min(
                    circuitArea.Width / (double)(bounds.Width + 140),
                    circuitArea.Height / (double)(bounds.Height + 100));
            }
            scale = Math.Min(scale, 1.5); // Limit scale so we don't create enormous circuits in big windows

            /* calculate transform so circuit fills most of screen */
            transform[0] = transform[3] = (float)scale;
            transform[1] = transform[2] = transform[4] = transform[5] = 0;
            if (0 < bounds.Width) {
                transform[4] = (float)((circuitArea.Width - bounds.Width * scale) / 2 - bounds.X * scale);
                transform[5] = (float)((circuitArea.Height - bounds.Height * scale) / 2 - bounds.Y * scale);
            }
        }

        /* get circuit bounds.  remember this doesn't use setBbox().  That is calculated when we draw */
        /* the circuit, but this needs to be ready before we first draw it, so we use this crude method */
        Rectangle getCircuitBounds() {
            int i;
            int minx = 1000, maxx = 0, miny = 1000, maxy = 0;
            for (i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                /* centered text causes problems when trying to center the circuit, */
                /* so we special-case it here */
                if (!ce.IsCenteredText) {
                    minx = Math.Min(ce.X1, Math.Min(ce.X2, minx));
                    maxx = Math.Max(ce.X1, Math.Max(ce.X2, maxx));
                }
                miny = Math.Min(ce.Y1, Math.Min(ce.Y2, miny));
                maxy = Math.Max(ce.Y1, Math.Max(ce.Y2, maxy));
            }
            if (minx > maxx) {
                return new Rectangle();
            }
            return new Rectangle(minx, miny, maxx - minx, maxy - miny);
        }

        void stackScope(int s) {
            if (s == 0) {
                if (scopeCount < 2) {
                    return;
                }
                s = 1;
            }
            if (scopes[s].Position == scopes[s - 1].Position) {
                return;
            }
            scopes[s].Position = scopes[s - 1].Position;
            for (s++; s < scopeCount; s++) {
                scopes[s].Position--;
            }
        }

        void unstackScope(int s) {
            if (s == 0) {
                if (scopeCount < 2) {
                    return;
                }
                s = 1;
            }
            if (scopes[s].Position != scopes[s - 1].Position) {
                return;
            }
            for (; s < scopeCount; s++) {
                scopes[s].Position++;
            }
        }

        void combineScope(int s) {
            if (s == 0) {
                if (scopeCount < 2) {
                    return;
                }
                s = 1;
            }
            scopes[s - 1].Combine(scopes[s]);
            scopes[s].SetElm(null);
        }

        void stackAll() {
            for (int i = 0; i != scopeCount; i++) {
                scopes[i].Position = 0;
                scopes[i].ShowMax = false;
                scopes[i].ShowMin = false;
            }
        }

        void unstackAll() {
            for (int i = 0; i != scopeCount; i++) {
                scopes[i].Position = i;
                scopes[i].ShowMax = true;
            }
        }

        void combineAll() {
            for (int i = scopeCount - 2; i >= 0; i--) {
                scopes[i].Combine(scopes[i + 1]);
                scopes[i + 1].SetElm(null);
            }
        }

        void separateAll() {
            var newscopes = new List<Scope>();
            int ct = 0;
            for (int i = 0; i < scopeCount; i++) {
                ct = scopes[i].Separate(newscopes, ct);
            }
            scopes = newscopes.ToArray();
            scopeCount = ct;
        }

        void doEdit(Editable eable) {
            clearSelection();
            pushUndo();
            if (editDialog != null) {
                editDialog.closeDialog();
                editDialog = null;
            }
            editDialog = new ElementInfoDialog(eable, this);
            editDialog.Show(
                mouseCursorX + mParent.Location.X,
                mouseCursorY + mParent.Location.Y
            );
        }

        void doSliders(CircuitElm ce) {
            clearSelection();
            pushUndo();
            if (sliderDialog != null) {
                sliderDialog.closeDialog();
                sliderDialog = null;
            }
            sliderDialog = new SliderDialog(ce, this);
            sliderDialog.Show(mParent.Left + mouseCursorX, mParent.Top + mouseCursorY);
        }

        void doCreateSubcircuit() {
            var dlg = new EditCompositeModelDialog();
            if (!dlg.createModel()) {
                return;
            }
            dlg.createDialog();
            dialogShowing = dlg;
            dialogShowing.Show();
        }

        void doOpenFile() {
            var open = new OpenFileDialog();
            open.Filter = "テキストファイル(*.txt)|*.txt";
            open.ShowDialog();
            if (string.IsNullOrEmpty(open.FileName) || !Directory.Exists(Path.GetDirectoryName(open.FileName))) {
                return;
            }
            pushUndo();
            var fs = new StreamReader(open.FileName);
            var data = fs.ReadToEnd();
            fs.Close();
            fs.Dispose();
            readCircuit(data);
        }

        void doSaveFile() {
            var save = new SaveFileDialog();
            save.Filter = "テキストファイル(*.txt)|*.txt";
            save.ShowDialog();
            if (string.IsNullOrEmpty(save.FileName) || !Directory.Exists(Path.GetDirectoryName(save.FileName))) {
                return;
            }
            string dump = dumpCircuit();
            var fs = new StreamWriter(save.FileName);
            fs.Write(dump);
            fs.Close();
            fs.Dispose();
        }

        string dumpCircuit() {
            CustomLogicModel.clearDumpedFlags();
            CustomCompositeModel.clearDumpedFlags();
            DiodeModel.clearDumpedFlags();

            int f = chkShowDots.Checked ? 1 : 0;
            f |= chkShowVolts.Checked ? 0 : 4;
            f |= chkShowValues.Checked ? 0 : 16;

            /* 32 = linear scale in afilter */
            string dump = "$ " + f
                + " " + timeStep
                + " " + getIterCount()
                + " " + trbCurrentBar.Value
                + " " + CircuitElm.VoltageRange + "\n";

            int i;
            for (i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                string m = ce.DumpModel();
                if (!string.IsNullOrEmpty(m)) {
                    dump += m + "\n";
                }
                dump += ce.Dump + "\n";
            }
            for (i = 0; i != scopeCount; i++) {
                string d = scopes[i].Dump();
                if (d != null) {
                    dump += d + "\n";
                }
            }
            for (i = 0; i != adjustables.Count; i++) {
                var adj = adjustables[i];
                dump += "38 " + adj.dump() + "\n";
            }
            if (Hint.Type != -1) {
                dump += "h " + Hint.Type + " " + Hint.Item1 + " " +
                Hint.Item2 + "\n";
            }

            return dump;
        }

        void getSetupList(bool openDefault) {
            // TODO: getSetupList
            //string url;
            //url = GWT.getModuleBaseURL() + "setuplist.txt" + "?v=" + random.nextInt();
            //var requestBuilder = new RequestBuilder(RequestBuilder.GET, url);
            //try {
            //    requestBuilder.sendRequest(null, new RequestCallback() {
            //        public void onError(Request request, Throwable exception) {
            //            Console.WriteLine("File Error Response", exception);
            //        }

            //        public void onResponseReceived(Request request, Response response) {
            //            // processing goes here
            //            if (response.getStatusCode() == Response.SC_OK) {
            //                string text = response.getText();
            //                processSetupList(text.getBytes(), openDefault);
            //                // end or processing
            //            } else {
            //                Console.WriteLine("Bad file server response:" + response.getStatusText());
            //            }
            //        }
            //    });
            //} catch (Exception e) {
            //    Console.WriteLine("failed file reading", e);
            //}
        }

        void processSetupList(byte[] b, bool openDefault) {
            int len = b.Length;
            ToolStripItem currentMenuBar;
            currentMenuBar = new ToolStripMenuItem() {
                Text = "Circuits"
            };
            menuBar.Items.Add(currentMenuBar);
            var stack = new ToolStripItem[6];
            int stackptr = 0;
            stack[stackptr++] = currentMenuBar;
            int p;
            for (p = 0; p < len;) {
                int l;
                for (l = 0; l != len - p; l++) {
                    if (b[l + p] == '\n') {
                        l++;
                        break;
                    }
                }
                string line = Encoding.ASCII.GetString(b, p, l - 1);
                if (line.ElementAt(0) == '#') {
                } else if (line.ElementAt(0) == '+') {
                    var n = new ToolStripMenuItem() {
                        Text = line.Substring(1)
                    };
                    ((ToolStripMenuItem)currentMenuBar).DropDownItems.Add(n);
                    currentMenuBar = stack[stackptr++] = n;
                } else if (line.ElementAt(0) == '-') {
                    currentMenuBar = stack[--stackptr - 1];
                } else {
                    int i = line.IndexOf(' ');
                    if (i > 0) {
                        string title = line.Substring(i + 1);
                        bool first = false;
                        if (line.ElementAt(0) == '>') {
                            first = true;
                        }
                        string file = line.Substring(first ? 1 : 0, i);
                        var item = new ToolStripMenuItem() {
                            Text = title
                        };
                        item.Click += new EventHandler((sender, e) => {
                            MenuPerformed(MENU_CATEGORY.CIRCUITS, MENU_ITEM.SETUP, file + " " + title);
                        });
                        ((ToolStripMenuItem)currentMenuBar).DropDownItems.Add(item);
                        if (file == startCircuit && startLabel == null) {
                            startLabel = title;
                        }
                        if (first && startCircuit == null) {
                            startCircuit = file;
                            startLabel = title;
                            if (openDefault && mCir.StopMessage == null) {
                                readSetupFile(startCircuit, startLabel);
                            }
                        }
                    }
                }
                p += l;
            }
        }

        void readCircuit(string text, int flags) {
            readCircuit(Encoding.ASCII.GetBytes(text), flags);
        }

        void readCircuit(string text) {
            readCircuit(Encoding.ASCII.GetBytes(text), 0);
        }

        void readSetupFile(string str, string title) {
            t = 0;
            Console.WriteLine(str);
            // TODO: Maybe think about some better approach to cache management!
            //string url = GWT.getModuleBaseURL() + "circuits/" + str + "?v=" + random.nextInt();
            //loadFileFromURL(url);
            //if (title != null) {
            //    titleLabel.Text = title;
            //}
            //unsavedChanges = false;
        }

        void readCircuit(byte[] b, int flags) {
            Console.WriteLine("readCircuit");
            int i;
            int len = b.Length;
            if ((flags & RC_RETAIN) == 0) {
                clearMouseElm();
                for (i = 0; i != elmList.Count; i++) {
                    var ce = getElm(i);
                    ce.Delete();
                }
                elmList.Clear();
                Hint.Type = -1;
                timeStep = 10e-6;
                chkShowDots.Checked = false;
                chkShowVolts.Checked = true;
                chkShowValues.Checked = true;
                chkUseAnsiSymbols.Checked = true;
                setGrid();
                trbSpeedBar.Value = 57;
                trbCurrentBar.Value = 50;
                CircuitElm.VoltageRange = 5;
                scopeCount = 0;
                lastIterTime = 0;
            }

            bool subs = (flags & RC_SUBCIRCUITS) != 0;

            int p;
            for (p = 0; p < len;) {
                int l;
                int linelen = len - p; /* IES - changed to allow the last line to not end with a delim. */
                for (l = 0; l != len - p; l++) {
                    if (b[l + p] == '\n' || b[l + p] == '\r') {
                        linelen = l++;
                        if (l + p < b.Length && b[l + p] == '\n') {
                            l++;
                        }
                        break;
                    }
                }
                string line = Encoding.ASCII.GetString(b, p, linelen);
                var st = new StringTokenizer(line, " +\t\n\r\f");
                while (st.hasMoreTokens()) {
                    string type = st.nextToken();
                    int tint = type.ElementAt(0);
                    try {
                        if (subs && tint != '.') {
                            continue;
                        }
                        if (tint == 'o') {
                            var sc = new Scope(this);
                            sc.Position = scopeCount;
                            sc.Undump(st);
                            scopes[scopeCount++] = sc;
                            break;
                        }
                        if (tint == 'h') {
                            readHint(st);
                            break;
                        }
                        if (tint == '$') {
                            readOptions(st);
                            break;
                        }
                        if (tint == '!') {
                            CustomLogicModel.undumpModel(st);
                            break;
                        }
                        if (tint == '%' || tint == '?' || tint == 'B') {
                            /* ignore afilter-specific stuff */
                            break;
                        }
                        /* do not add new symbols here without testing export as link */

                        /* if first character is a digit then parse the type as a number */
                        if (tint >= '0' && tint <= '9') {
                            tint = int.Parse(type);
                        }
                        if (tint == 34) {
                            DiodeModel.undumpModel(st);
                            break;
                        }
                        if (tint == 38) {
                            var adj = new Adjustable(st, this);
                            adjustables.Add(adj);
                            break;
                        }
                        if (tint == '.') {
                            CustomCompositeModel.undumpModel(st);
                            break;
                        }
                        int x1 = st.nextTokenInt();
                        int y1 = st.nextTokenInt();
                        int x2 = st.nextTokenInt();
                        int y2 = st.nextTokenInt();
                        int f = st.nextTokenInt();
                        var dumpId = MenuItems.getDumpIdFromString(type);
                        var newce = MenuItems.createCe(dumpId, x1, y1, x2, y2, f, st);
                        if (newce == null) {
                            Console.WriteLine("unrecognized dump type: " + type);
                            break;
                        }
                        newce.SetPoints();
                        elmList.Add(newce);
                    } catch (Exception ee) {
                        Console.WriteLine("exception while undumping " + ee);
                        Console.WriteLine(ee.StackTrace);
                        break;
                    }
                    break;
                }
                p += l;
            }

            enableItems();
            if ((flags & RC_RETAIN) == 0) {
                /* create sliders as needed */
                for (i = 0; i != adjustables.Count; i++) {
                    adjustables[i].createSlider(this);
                }
            }
            NeedAnalyze();
            if ((flags & RC_NO_CENTER) == 0) {
                centreCircuit();
            }
            if ((flags & RC_SUBCIRCUITS) != 0) {
                UpdateModels();
            }
            // TODO: readCircuit
            //AudioInputElm.clearCache();  /* to save memory */
        }

        void readHint(StringTokenizer st) {
            Hint.Type = st.nextTokenInt();
            Hint.Item1 = st.nextTokenInt();
            Hint.Item2 = st.nextTokenInt();
        }

        void readOptions(StringTokenizer st) {
            int flags = st.nextTokenInt();
            chkShowDots.Checked = (flags & 1) != 0;
            chkShowVolts.Checked = (flags & 4) == 0;
            chkShowValues.Checked = (flags & 16) == 0;

            timeStep = st.nextTokenDouble();
            double sp = st.nextTokenDouble();
            int sp2 = (int)(Math.Log(10 * sp) * 24 + 61.5);
            trbSpeedBar.Value = sp2;
            trbCurrentBar.Value = st.nextTokenInt();
            CircuitElm.VoltageRange = st.nextTokenDouble();

            setGrid();
        }

        public int snapGrid(int x) {
            return (x + gridRound) & gridMask;
        }

        bool doSwitch(int x, int y) {
            if (mouseElm == null || !(mouseElm is SwitchElm)) {
                return false;
            }
            var se = (SwitchElm)mouseElm;
            if (!se.getSwitchRect().Contains(x, y)) {
                return false;
            }
            se.toggle();
            if (se.momentary) {
                heldSwitchElm = se;
            }
            NeedAnalyze();
            return true;
        }

        void mouseDragged() {
            /* ignore right mouse button with no modifiers (needed on PC) */
            if (mouseButton == MouseButtons.Right) {
                return;
            }

            if (tempMouseMode == MOUSE_MODE.DRAG_SPLITTER) {
                dragSplitter(mouseCursorX, mouseCursorY);
                return;
            }
            int gx = inverseTransformX(mouseCursorX);
            int gy = inverseTransformY(mouseCursorY);
            if (!circuitArea.Contains(mouseCursorX, mouseCursorY)) {
                return;
            }
            bool changed = false;
            if (dragElm != null) {
                dragElm.Drag(gx, gy);
            }
            bool success = true;
            switch (tempMouseMode) {
            case MOUSE_MODE.DRAG_ALL:
                dragAll(mouseCursorX, mouseCursorY);
                break;
            case MOUSE_MODE.DRAG_ROW:
                dragRow(snapGrid(gx), snapGrid(gy));
                changed = true;
                break;
            case MOUSE_MODE.DRAG_COLUMN:
                dragColumn(snapGrid(gx), snapGrid(gy));
                changed = true;
                break;
            case MOUSE_MODE.DRAG_POST:
                if (mouseElm != null) {
                    dragPost(snapGrid(gx), snapGrid(gy));
                    changed = true;
                }
                break;
            case MOUSE_MODE.SELECT:
                if (mouseElm == null) {
                    selectArea(gx, gy);
                } else {
                    /* wait short delay before dragging.  This is to fix problem where switches were accidentally getting */
                    /* dragged when tapped on mobile devices */
                    if (DateTime.Now.ToFileTimeUtc() - mouseDownTime < 150) {
                        return;
                    }
                    tempMouseMode = MOUSE_MODE.DRAG_SELECTED;
                    changed = success = dragSelected(gx, gy);
                }
                break;
            case MOUSE_MODE.DRAG_SELECTED:
                changed = success = dragSelected(gx, gy);
                break;
            }
            dragging = true;
            if (success) {
                dragScreenX = mouseCursorX;
                dragScreenY = mouseCursorY;
                /* Console.WriteLine("setting dragGridx in mousedragged");*/
                dragGridX = inverseTransformX(dragScreenX);
                dragGridY = inverseTransformY(dragScreenY);
                if (!(tempMouseMode == MOUSE_MODE.DRAG_SELECTED && onlyGraphicsElmsSelected())) {
                    dragGridX = snapGrid(dragGridX);
                    dragGridY = snapGrid(dragGridY);
                }
            }
            if (changed) {
                writeRecoveryToStorage();
            }
            Repaint();
        }

        void dragSplitter(int x, int y) {
            double h = picCir.Height;
            if (h < 1) {
                h = 1;
            }
            scopeHeightFraction = 1.0 - (y / h);
            if (scopeHeightFraction < 0.1) {
                scopeHeightFraction = 0.1;
            }
            if (scopeHeightFraction > 0.9) {
                scopeHeightFraction = 0.9;
            }
            setCircuitArea();
            Repaint();
        }

        void dragAll(int x, int y) {
            int dx = x - dragScreenX;
            int dy = y - dragScreenY;
            if (dx == 0 && dy == 0) {
                return;
            }
            transform[4] += dx;
            transform[5] += dy;
            dragScreenX = x;
            dragScreenY = y;
        }

        void dragRow(int x, int y) {
            int dy = y - dragGridY;
            if (dy == 0) {
                return;
            }
            for (int i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                if (ce.Y1 == dragGridY) {
                    ce.MovePoint(0, 0, dy);
                }
                if (ce.Y2 == dragGridY) {
                    ce.MovePoint(1, 0, dy);
                }
            }
            removeZeroLengthElements();
        }

        void dragColumn(int x, int y) {
            int dx = x - dragGridX;
            if (dx == 0) {
                return;
            }
            for (int i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                if (ce.X1 == dragGridX) {
                    ce.MovePoint(0, dx, 0);
                }
                if (ce.X2 == dragGridX) {
                    ce.MovePoint(1, dx, 0);
                }
            }
            removeZeroLengthElements();
        }

        bool onlyGraphicsElmsSelected() {
            if (mouseElm != null && !(mouseElm is GraphicElm)) {
                return false;
            }
            for (int i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                if (ce.IsSelected && !(ce is GraphicElm)) {
                    return false;
                }
            }
            return true;
        }

        bool dragSelected(int x, int y) {
            bool me = false;
            int i;
            if (mouseElm != null && !mouseElm.IsSelected) {
                mouseElm.IsSelected = me = true;
            }
            if (!onlyGraphicsElmsSelected()) {
                Console.WriteLine("Snapping x and y");
                x = snapGrid(x);
                y = snapGrid(y);
            }
            int dx = x - dragGridX;
            int dy = y - dragGridY;
            if (dx == 0 && dy == 0) {
                /* don't leave mouseElm selected if we selected it above */
                if (me) {
                    mouseElm.IsSelected = false;
                }
                return false;
            }
            /* check if moves are allowed */
            bool allowed = true;
            for (i = 0; allowed && i != elmList.Count; i++) {
                var ce = getElm(i);
                if (ce.IsSelected && !ce.AllowMove(dx, dy)) {
                    allowed = false;
                }
            }
            if (allowed) {
                for (i = 0; i != elmList.Count; i++) {
                    var ce = getElm(i);
                    if (ce.IsSelected) {
                        ce.Move(dx, dy);
                    }
                }
                NeedAnalyze();
            }
            /* don't leave mouseElm selected if we selected it above */
            if (me) {
                mouseElm.IsSelected = false;
            }

            return allowed;
        }

        void dragPost(int x, int y) {
            if (draggingPost == -1) {
                draggingPost
                    = (Utils.Distance(mouseElm.X1, mouseElm.Y1, x, y)
                    > Utils.Distance(mouseElm.X2, mouseElm.Y2, x, y))
                    ? 1 : 0;
            }
            int dx = x - dragGridX;
            int dy = y - dragGridY;
            if (dx == 0 && dy == 0) {
                return;
            }
            mouseElm.MovePoint(draggingPost, dx, dy);
            NeedAnalyze();
        }

        void doFlip() {
            menuElm.FlipPosts();
            NeedAnalyze();
        }

        void doSplit(CircuitElm ce) {
            int x = snapGrid(inverseTransformX(menuX));
            int y = snapGrid(inverseTransformY(menuY));
            if (ce == null || !(ce is WireElm)) {
                return;
            }
            if (ce.X1 == ce.X2) {
                x = ce.X1;
            } else {
                y = ce.Y1;
            }
            /* don't create zero-length wire */
            if (x == ce.X1 && y == ce.Y1 || x == ce.X2 && y == ce.Y2) {
                return;
            }
            var newWire = new WireElm(x, y);
            newWire.Drag(ce.X2, ce.Y2);
            ce.Drag(x, y);
            elmList.Add(newWire);
            NeedAnalyze();
        }

        void selectArea(int x, int y) {
            int x1 = Math.Min(x, initDragGridX);
            int x2 = Math.Max(x, initDragGridX);
            int y1 = Math.Min(y, initDragGridY);
            int y2 = Math.Max(y, initDragGridY);
            selectedArea = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            for (int i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                ce.SelectRect(selectedArea);
            }
        }

        void setMouseElm(CircuitElm ce) {
            if (ce != mouseElm) {
                if (mouseElm != null) {
                    mouseElm.SetMouseElm(false);
                }
                if (ce != null) {
                    ce.SetMouseElm(true);
                }
                mouseElm = ce;
            }
        }

        void removeZeroLengthElements() {
            for (int i = elmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                if (ce.X1 == ce.X2 && ce.Y1 == ce.Y2) {
                    elmList.RemoveAt(i);
                    /*Console.WriteLine("delete element: {0} {1}\t{2} {3}\t{4}", ce.GetType(), ce.x1, ce.y1, ce.x2, ce.y2); */
                    ce.Delete();
                }
            }
            NeedAnalyze();
        }

        bool mouseIsOverSplitter(int x, int y) {
            bool isOverSplitter = (x >= 0)
                && (x < circuitArea.Width)
                && (y >= circuitArea.Height - 10)
                && (y <= circuitArea.Height + 5);
            if (isOverSplitter != mouseWasOverSplitter) {
                if (isOverSplitter) {
                    setCursorStyle(Cursors.HSplit);
                } else {
                    setMouseMode(mouseMode);
                }
            }
            mouseWasOverSplitter = isOverSplitter;
            return isOverSplitter;
        }

        /* convert screen coordinates to grid coordinates by inverting circuit transform */
        int inverseTransformX(double x) {
            return (int)((x - transform[4]) / transform[0]);
        }
        int inverseTransformY(double y) {
            return (int)((y - transform[5]) / transform[3]);
        }

        /* need to break this out into a separate routine to handle selection, */
        /* since we don't get mouse move events on mobile */
        void mouseSelect() {
            CircuitElm newMouseElm = null;
            int mx = mouseCursorX;
            int my = mouseCursorY;
            int gx = inverseTransformX(mx);
            int gy = inverseTransformY(my);

            /*Console.WriteLine("Settingd draggridx in mouseEvent");*/
            dragGridX = snapGrid(gx);
            dragGridY = snapGrid(gy);
            dragScreenX = mx;
            dragScreenY = my;
            draggingPost = -1;

            mousePost = -1;
            plotXElm = plotYElm = null;

            if (mouseIsOverSplitter(mx, my)) {
                setMouseElm(null);
                return;
            }

            double minDistance = 8;
            for (int i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                var distance = ce.Distance(gx, gy);
                if (distance < minDistance || ce.BoundingBox.IntersectsWith(new Rectangle(gx - 1, gy - 1, 2, 2))) {
                    newMouseElm = ce;
                    minDistance = distance;
                }
            }

            scopeSelected = -1;
            if (newMouseElm == null) {
                for (int i = 0; i != scopeCount; i++) {
                    var s = scopes[i];
                    if (s.BoundingBox.Contains(mx, my)) {
                        newMouseElm = s.Elm;
                        if (s.PlotXY) {
                            plotXElm = s.Elm;
                            plotYElm = s.YElm;
                        }
                        scopeSelected = i;
                    }
                }
                /* the mouse pointer was not in any of the bounding boxes, but we
                /* might still be close to a post */
                for (int i = 0; i != elmList.Count; i++) {
                    var ce = getElm(i);
                    if (mouseMode == MOUSE_MODE.DRAG_POST) {
                        if (ce.GetHandleGrabbedClose(gx, gy, POSTGRABSQ, 0) > 0) {
                            newMouseElm = ce;
                            break;
                        }
                    }
                    int jn = ce.PostCount;
                    for (int j = 0; j != jn; j++) {
                        var pt = ce.GetPost(j);
                        if (Utils.Distance(pt, gx, gy) < 26) {
                            newMouseElm = ce;
                            mousePost = j;
                            break;
                        }
                    }
                }
            } else {
                mousePost = -1;
                /* look for post close to the mouse pointer */
                for (int i = 0; i != newMouseElm.PostCount; i++) {
                    var pt = newMouseElm.GetPost(i);
                    if (Utils.Distance(pt, gx, gy) < 26) {
                        mousePost = i;
                    }
                }
            }
            Repaint();
            setMouseElm(newMouseElm);
        }

        void onContextMenu(Control ctrl, MouseEventArgs e) {
            menuClientX = mParent.Location.X + e.X;
            menuClientY = mParent.Location.Y + e.Y;
            doPopupMenu();
        }

        void doPopupMenu() {
            menuElm = mouseElm;
            menuScope = -1;
            menuPlot = -1;
            if (scopeSelected != -1) {
                if (scopes[scopeSelected].CanMenu) {
                    menuScope = scopeSelected;
                    menuPlot = scopes[scopeSelected].SelectedPlot;
                    scopePopupMenu.doScopePopupChecks(false, scopes[scopeSelected]);
                    contextPanel = new ContextMenuStrip();
                    contextPanel.Items.AddRange(scopePopupMenu.getMenuBar());
                    var y = Math.Max(0, Math.Min(menuClientY, cv.Height - 160));
                    contextPanel.Show();
                    contextPanel.Location = new Point(menuClientX, y);
                }
            } else if (mouseElm != null) {
                if (!(mouseElm is ScopeElm)) {
                    elmScopeMenuItem.Enabled = mouseElm.CanViewInScope;
                    elmFloatScopeMenuItem.Enabled = mouseElm.CanViewInScope;
                    elmEditMenuItem.Enabled = mouseElm.GetElementInfo(0) != null;
                    elmFlipMenuItem.Enabled = 2 == mouseElm.PostCount;
                    elmSplitMenuItem.Enabled = canSplit(mouseElm);
                    elmSliderMenuItem.Enabled = sliderItemEnabled(mouseElm);
                    contextPanel = new ContextMenuStrip();
                    contextPanel.Items.AddRange(elmMenuBar);
                    contextPanel.Show();
                    contextPanel.Location = new Point(menuClientX, menuClientY);
                } else {
                    var s = (ScopeElm)mouseElm;
                    if (s.elmScope.CanMenu) {
                        menuPlot = s.elmScope.SelectedPlot;
                        scopePopupMenu.doScopePopupChecks(true, s.elmScope);
                        contextPanel = new ContextMenuStrip();
                        contextPanel.Items.AddRange(scopePopupMenu.getMenuBar());
                        contextPanel.Show();
                        contextPanel.Location = new Point(menuClientX, menuClientY);
                    }
                }
            }
        }

        bool canSplit(CircuitElm ce) {
            if (!(ce is WireElm)) {
                return false;
            }
            var we = (WireElm)ce;
            if (we.X1 == we.X2 || we.Y1 == we.Y2) {
                return true;
            }
            return false;
        }

        /* check if the user can create sliders for this element */
        bool sliderItemEnabled(CircuitElm elm) {
            /* prevent confusion */
            if (elm is PotElm) {
                return false;
            }
            for (int i = 0; ; i++) {
                var ei = elm.GetElementInfo(i);
                if (ei == null) {
                    return false;
                }
                if (ei.CanCreateAdjustable()) {
                    return true;
                }
            }
        }

        void longPress() {
            doPopupMenu();
        }

        void clearMouseElm() {
            scopeSelected = -1;
            setMouseElm(null);
            plotXElm = plotYElm = null;
        }

        void doMainMenuChecks() {
            int c = Menu.mainMenuItems.Count;
            for (int i = 0; i < c; i++) {
                Menu.mainMenuItems[i].Checked = Menu.mainMenuItemNames[i] == mouseModeStr;
            }
        }

        void zoomCircuit(int dy) {
            double newScale;
            double oldScale = transform[0];
            double val = dy * .01;
            newScale = Math.Max(oldScale + val, .2);
            newScale = Math.Min(newScale, 2.5);
            setCircuitScale(newScale);
        }

        void setCircuitScale(double newScale) {
            int cx = inverseTransformX(circuitArea.Width / 2);
            int cy = inverseTransformY(circuitArea.Height / 2);
            transform[0] = transform[3] = (float)newScale;
            /* adjust translation to keep center of screen constant
            /* inverse transform = (x-t4)/t0 */
            transform[4] = (float)(circuitArea.Width / 2 - cx * newScale);
            transform[5] = (float)(circuitArea.Height / 2 - cy * newScale);
        }

        void scrollValues(int deltay) {
            if (mouseElm != null && !DialogIsShowing() && scopeSelected == -1) {
                if ((mouseElm is ResistorElm) || (mouseElm is CapacitorElm) || (mouseElm is InductorElm)) {
                    scrollValuePopup = new ScrollValuePopup(deltay, mouseElm, this);
                    scrollValuePopup.Show(
                        mParent.Location.X + mouseCursorX,
                        mParent.Location.Y + mouseCursorY
                    );
                }
            }
        }

        void enableItems() { }

        void setGrid() {
            gridSize = 8;
            gridMask = ~(gridSize - 1);
            gridRound = gridSize / 2 - 1;
        }

        public void pushUndo() {
            redoStack.Clear();
            string s = dumpCircuit();
            if (undoStack.Count > 0 && s == undoStack[undoStack.Count - 1]) {
                return;
            }
            undoStack.Add(s);
            enableUndoRedo();
        }

        void doUndo() {
            if (undoStack.Count == 0) {
                return;
            }
            redoStack.Add(dumpCircuit());
            string tmp = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            readCircuit(tmp, RC_NO_CENTER);
            enableUndoRedo();
        }

        void doRedo() {
            if (redoStack.Count == 0) {
                return;
            }
            undoStack.Add(dumpCircuit());
            string tmp = redoStack[redoStack.Count - 1];
            redoStack.RemoveAt(redoStack.Count - 1);
            readCircuit(tmp, RC_NO_CENTER);
            enableUndoRedo();
        }

        void doRecover() {
            pushUndo();
            readCircuit(recovery);
            recoverItem.Enabled = false;
        }

        void enableUndoRedo() {
            redoItem.Enabled = redoStack.Count > 0;
            undoItem.Enabled = undoStack.Count > 0;
        }

        void setMouseMode(MOUSE_MODE mode) {
            mouseMode = mode;
            if (mode == MOUSE_MODE.ADD_ELM) {
                setCursorStyle(Cursors.Cross);
            } else {
                setCursorStyle(Cursors.Arrow);
            }
        }

        void setCursorStyle(Cursor s) {
            lastCursorStyle = Cursor.Current;
            mParent.Cursor = s;
        }

        void setMenuSelection() {
            if (menuElm != null) {
                if (menuElm.IsSelected) {
                    return;
                }
                clearSelection();
                menuElm.IsSelected = true;
            }
        }

        void doCut() {
            int i;
            pushUndo();
            setMenuSelection();
            clipboard = "";
            for (i = elmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                /* ScopeElms don't cut-paste well because their reference to a parent
                /* elm by number get's messed up in the dump. For now we will just ignore them
                /* until I can be bothered to come up with something better */
                if (willDelete(ce) && !(ce is ScopeElm)) {
                    clipboard += ce.Dump + "\n";
                }
            }
            writeClipboardToStorage();
            doDelete(true);
            enablePaste();
        }

        void writeClipboardToStorage() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            stor.setItem("circuitClipboard", clipboard);
        }

        void readClipboardFromStorage() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            clipboard = stor.getItem("circuitClipboard");
        }

        void writeRecoveryToStorage() {
            Console.WriteLine("write recovery");
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            string s = dumpCircuit();
            stor.setItem("circuitRecovery", s);
        }

        void readRecovery() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            recovery = stor.getItem("circuitRecovery");
        }

        void deleteUnusedScopeElms() {
            /* Remove any scopeElms for elements that no longer exist */
            for (int i = elmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                if ((ce is ScopeElm) && ((ScopeElm)ce).elmScope.NeedToRemove) {
                    ce.Delete();
                    elmList.RemoveAt(i);
                }
            }
        }

        void doDelete(bool pushUndoFlag) {
            int i;
            if (pushUndoFlag) {
                pushUndo();
            }
            bool hasDeleted = false;

            for (i = elmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                if (willDelete(ce)) {
                    if (ce.IsMouseElm) {
                        setMouseElm(null);
                    }
                    ce.Delete();
                    elmList.RemoveAt(i);
                    hasDeleted = true;
                }
            }
            if (hasDeleted) {
                deleteUnusedScopeElms();
                NeedAnalyze();
                writeRecoveryToStorage();
            }
        }

        bool willDelete(CircuitElm ce) {
            /* Is this element in the list to be deleted.
            /* This changes the logic from the previous version which would initially only
            /* delete selected elements (which could include the mouseElm) and then delete the
            /* mouseElm if there were no selected elements. Not really sure this added anything useful
            /* to the user experience.
            /*
            /* BTW, the old logic could also leave mouseElm pointing to a deleted element. */
            return ce.IsSelected || ce.IsMouseElm;
        }

        string copyOfSelectedElms() {
            string r = "";
            CustomLogicModel.clearDumpedFlags();
            CustomCompositeModel.clearDumpedFlags();
            DiodeModel.clearDumpedFlags();
            for (int i = elmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                string m = ce.DumpModel();
                if (!string.IsNullOrEmpty(m)) {
                    r += m + "\n";
                }
                /* See notes on do cut why we don't copy ScopeElms. */
                if (ce.IsSelected && !(ce is ScopeElm)) {
                    r += ce.Dump + "\n";
                }
            }
            return r;
        }

        void doCopy() {
            /* clear selection when we're done if we're copying a single element using the context menu */
            bool clearSel = (menuElm != null && !menuElm.IsSelected);

            setMenuSelection();
            clipboard = copyOfSelectedElms();

            if (clearSel) {
                clearSelection();
            }
            writeClipboardToStorage();
            enablePaste();
        }

        void enablePaste() {
            if (string.IsNullOrEmpty(clipboard)) {
                readClipboardFromStorage();
            }
            pasteItem.Enabled = clipboard != null && clipboard.Length > 0;
        }

        void doDuplicate() {
            setMenuSelection();
            string s = copyOfSelectedElms();
            doPaste(s);
        }

        void doPaste(string dump) {
            pushUndo();
            clearSelection();
            int i;

            /* get old bounding box */
            var oldbb = new Rectangle();
            for (i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                var bb = ce.BoundingBox;
                if (0 == i) {
                    oldbb = bb;
                } else {
                    oldbb = Rectangle.Union(oldbb, bb);
                }
            }

            /* add new items */
            int oldsz = elmList.Count;
            if (dump != null) {
                readCircuit(dump, RC_RETAIN);
            } else {
                readClipboardFromStorage();
                readCircuit(clipboard, RC_RETAIN);
            }

            /* select new items and get their bounding box */
            var newbb = new Rectangle();
            for (i = oldsz; i != elmList.Count; i++) {
                var ce = getElm(i);
                ce.IsSelected = true;
                var bb = ce.BoundingBox;
                if (0 == i) {
                    newbb = bb;
                } else {
                    newbb = Rectangle.Union(newbb, bb);
                }
            }

            if (oldbb != null && newbb != null && oldbb.Contains(newbb)) {
                /* find a place on the edge for new items */
                int dx = 0, dy = 0;
                int spacew = circuitArea.Width - oldbb.Width - newbb.Width;
                int spaceh = circuitArea.Height - oldbb.Height - newbb.Height;
                if (spacew > spaceh) {
                    dx = snapGrid(oldbb.X + oldbb.Width - newbb.X + gridSize);
                } else {
                    dy = snapGrid(oldbb.Y + oldbb.Height - newbb.Y + gridSize);
                }

                /* move new items near the mouse if possible */
                if (mouseCursorX > 0 && circuitArea.Contains(mouseCursorX, mouseCursorY)) {
                    int gx = inverseTransformX(mouseCursorX);
                    int gy = inverseTransformY(mouseCursorY);
                    int mdx = snapGrid(gx - (newbb.X + newbb.Width / 2));
                    int mdy = snapGrid(gy - (newbb.Y + newbb.Height / 2));
                    for (i = oldsz; i != elmList.Count; i++) {
                        if (!getElm(i).AllowMove(mdx, mdy)) {
                            break;
                        }
                    }
                    if (i == elmList.Count) {
                        dx = mdx;
                        dy = mdy;
                    }
                }

                /* move the new items */
                for (i = oldsz; i != elmList.Count; i++) {
                    var ce = getElm(i);
                    ce.Move(dx, dy);
                }
            }
            NeedAnalyze();
            writeRecoveryToStorage();
        }

        void clearSelection() {
            for (int i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                ce.IsSelected = false;
            }
        }

        void doSelectAll() {
            for (int i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                ce.IsSelected = true;
            }
        }

        bool anySelectedButMouse() {
            for (int i = 0; i != elmList.Count; i++) {
                if (getElm(i) != mouseElm && getElm(i).IsSelected) {
                    return true;
                }
            }
            return false;
        }

        void onPreviewNativeEvent(KeyEventArgs e) {
            var code = e.KeyCode;

            if (DialogIsShowing()) {
                if (scrollValuePopup != null && scrollValuePopup.Visible) {
                    if (code == Keys.Escape || code == Keys.Space) {
                        scrollValuePopup.close(false);
                    }
                    if (code == Keys.Enter) {
                        scrollValuePopup.close(true);
                    }
                }
                if (editDialog != null && editDialog.Visible) {
                    if (code == Keys.Escape) {
                        editDialog.closeDialog();
                    }
                    if (code == Keys.Enter) {
                        editDialog.enterPressed();
                    }
                }
                return;
            }

            if (code == Keys.Back || code == Keys.Delete) {
                if (scopeSelected != -1 && null != scopes[scopeSelected]) {
                    /* Treat DELETE key with scope selected as "remove scope", not delete */
                    scopes[scopeSelected].SetElm(null);
                    scopeSelected = -1;
                } else {
                    menuElm = null;
                    pushUndo();
                    doDelete(true);
                }
            }

            if (code == Keys.Escape) {
                setMouseMode(MOUSE_MODE.SELECT);
                mouseModeStr = MENU_ITEM.SELECT;
                tempMouseMode = mouseMode;
            }

            if (e.KeyValue > 32 && e.KeyValue < 127) {
                var c = Menu.shortcuts[e.KeyValue];
                if (c == MENU_ITEM.INVALID) {
                    return;
                }
                setMouseMode(MOUSE_MODE.ADD_ELM);
                mouseModeStr = c;
                tempMouseMode = mouseMode;
            }
            if (e.KeyValue == 32) {
                setMouseMode(MOUSE_MODE.SELECT);
                mouseModeStr = MENU_ITEM.SELECT;
                tempMouseMode = mouseMode;
            }
        }

        /* For debugging */
        void dumpNodelist() {
            CircuitElm e;
            int i, j;
            string s;
            string cs;
            Console.WriteLine("Elm list Dump");
            for (i = 0; i < elmList.Count; i++) {
                e = elmList[i];
                cs = e.ToString();
                int p = cs.LastIndexOf('.');
                cs = cs.Substring(p + 1);
                if (cs == "WireElm") {
                    continue;
                }
                if (cs == "TransistorElm") {
                    if (((TransistorElm)e).pnp == -1) {
                        cs = "PTransistorElm";
                    } else {
                        cs = "NTransistorElm";
                    }
                }
                s = cs;
                for (j = 0; j < e.PostCount; j++) {
                    s = s + " " + e.Nodes[j];
                }
                Console.WriteLine(s);
            }
        }

        void doDCAnalysis() {
            dcAnalysisFlag = true;
            resetButton_onClick();
        }

        bool isSelection() {
            for (int i = 0; i != elmList.Count; i++) {
                if (getElm(i).IsSelected) {
                    return true;
                }
            }
            return false;
        }

        public CustomCompositeModel getCircuitAsComposite() {
            int i;
            string nodeDump = "";
            string dump = "";
            CustomLogicModel.clearDumpedFlags();
            DiodeModel.clearDumpedFlags();
            var extList = new List<ExtListEntry>();

            bool sel = isSelection();

            // mapping of node labels -> node numbers
            var nodeNameHash = new Dictionary<string, int>();

            // mapping of node numbers -> equivalent node numbers (if they both have the same label)
            var nodeNumberHash = new Dictionary<int, int>();

            var used = new bool[mCir.NodeList.Count];

            // output all the elements
            for (i = 0; i != elmList.Count; i++) {
                var ce = getElm(i);
                if (sel && !ce.IsSelected) {
                    continue;
                }
                // don't need these elements dumped
                if ((ce is WireElm) || (ce is ScopeElm)) {
                    continue;
                }
                if (ce is GraphicElm) {
                    continue;
                }
                int j;
                if (nodeDump.Length > 0) {
                    nodeDump += "\r";
                }
                nodeDump += ce.GetType().ToString();
                for (j = 0; j != ce.PostCount; j++) {
                    int n = ce.Nodes[j];
                    int n0 = nodeNumberHash.ContainsKey(n) ? nodeNumberHash[n] : n;
                    used[n0] = true;
                    nodeDump += " " + n0;
                }

                // save positions
                int x1 = ce.X1;
                int y1 = ce.Y1;
                int x2 = ce.X2;
                int y2 = ce.Y2;

                // set them to 0 so they're easy to remove
                ce.X1 = ce.Y1 = ce.X2 = ce.Y2 = 0;

                string tstring = ce.Dump;
                var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 ");
                tstring = rg.Replace(tstring, "", 1); // remove unused tint_x1 y1 x2 y2 coords for internal components

                // restore positions
                ce.X1 = x1;
                ce.Y1 = y1;
                ce.X2 = x2;
                ce.Y2 = y2;
                if (dump.Length > 0) {
                    dump += " ";
                }
                dump += CustomLogicModel.escape(tstring);
            }

            for (i = 0; i != extList.Count; i++) {
                var ent = extList[i];
                if (!used[ent.node]) {
                    MessageBox.Show("Node \"" + ent.name + "\" is not used!");
                    return null;
                }
            }

            var ccm = new CustomCompositeModel();
            ccm.nodeList = nodeDump;
            ccm.elmDump = dump;
            ccm.extList = extList;
            return ccm;
        }
    }
}
