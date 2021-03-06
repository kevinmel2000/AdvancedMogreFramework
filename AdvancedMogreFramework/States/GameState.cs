﻿/*
-----------------------------------------------------------------------------
This source file is part of AdvancedMogreFramework
For the latest info, see https://github.com/cookgreen/AdvancedMogreFramework
Copyright (c) 2016-2020 Cook Green

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
-----------------------------------------------------------------------------
*/
using System;
using System.Collections.Generic;
using System.Text;
using Mogre;
using RMOgre;
using MOIS;
using Mogre_Procedural.MogreBites;

namespace AdvancedMogreFramework.States
{
    enum QueryFlags
    {
        OGRE_HEAD_MASK = 1 << 0,
        CUBE_MASK = 1 << 1
    };
    class GameState : AppState
    {
        public GameState()
        {
            m_MoveSpeed = 0.1f;
            m_RotateSpeed = 0.3f;

            m_bLMouseDown = false;
            m_bRMouseDown = false;
            m_bQuit = false;
            m_bSettingsMode = false;

            m_pDetailsPanel = null;
        }

        public override void enter()
        {
            AdvancedMogreFramework.Singleton.m_pLog.LogMessage("Entering GameState...");
            AdvancedMogreFramework.lastState = "GameState";
            m_pSceneMgr=AdvancedMogreFramework.Singleton.m_pRoot.CreateSceneManager(SceneType.ST_GENERIC, "GameSceneMgr");
            ColourValue cvAmbineLight=new ColourValue(0.7f,0.7f,0.7f);
            m_pSceneMgr.AmbientLight=cvAmbineLight;//(Ogre::ColourValue(0.7f, 0.7f, 0.7f));
 
            Ray r=new Ray();
            m_pRSQ = m_pSceneMgr.CreateRayQuery(r);
            m_pRSQ.QueryMask=1<<0;
 
            m_pCamera = m_pSceneMgr.CreateCamera("GameCamera");
            Mogre.Vector3 vectCameraPostion=new Mogre.Vector3(5,60,60);
            m_pCamera.Position=vectCameraPostion;
            Mogre.Vector3 vectorCameraLookAt=new Mogre.Vector3(5,20,0);
            m_pCamera.LookAt(vectorCameraLookAt);
            m_pCamera.NearClipDistance=5;
 
            m_pCamera.AspectRatio=AdvancedMogreFramework.Singleton.m_pViewport.ActualWidth / AdvancedMogreFramework.Singleton.m_pViewport.ActualHeight;

            AdvancedMogreFramework.Singleton.m_pViewport.Camera=m_pCamera;
            m_pCurrentObject = null;

 
            buildGUI();
 
            createScene();
        }
        public void createScene()
        {
            Mogre.Vector3 vectLightPos=new Mogre.Vector3(75,75,75);
            m_pSceneMgr.CreateLight("Light").Position = vectLightPos;//(75, 75, 75);

            DotSceneLoader pDotSceneLoader = new DotSceneLoader();
            pDotSceneLoader.ParseDotScene("CubeScene.xml", "General", m_pSceneMgr, m_pSceneMgr.RootSceneNode);
            pDotSceneLoader=null;

            m_pSceneMgr.GetEntity("Cube01").QueryFlags = 1 << 1;
            m_pSceneMgr.GetEntity("Cube02").QueryFlags=1<<1;//(CUBE_MASK);
            m_pSceneMgr.GetEntity("Cube03").QueryFlags=1<<1;//(CUBE_MASK);

            m_pOgreHeadEntity = m_pSceneMgr.CreateEntity("Cube", "ogrehead.mesh");
            m_pOgreHeadEntity.QueryFlags=1<<0;
            m_pOgreHeadNode = m_pSceneMgr.RootSceneNode.CreateChildSceneNode("CubeNode");
            m_pOgreHeadNode.AttachObject(m_pOgreHeadEntity);
            Mogre.Vector3 vectOgreHeadNodePos = new Mogre.Vector3(0,0,-25);
            m_pOgreHeadNode.Position = vectOgreHeadNodePos;// (Vector3(0, 0, -25));

            m_pOgreHeadMat = m_pOgreHeadEntity.GetSubEntity(1).GetMaterial();
            m_pOgreHeadMatHigh = m_pOgreHeadMat.Clone("OgreHeadMatHigh");
            ColourValue cvAmbinet = new Mogre.ColourValue(1, 0, 0);
            m_pOgreHeadMatHigh.GetTechnique(0).GetPass(0).Ambient = cvAmbinet;
            ColourValue cvDiffuse = new Mogre.ColourValue(1, 0, 0,0);
            m_pOgreHeadMatHigh.GetTechnique(0).GetPass(0).Diffuse = cvDiffuse;
        }
        public override void exit()
        {
            AdvancedMogreFramework.Singleton.m_pLog.LogMessage("Leaving GameState...");

            AdvancedMogreFramework.Singleton.m_pMouse.MouseMoved -= mouseMoved;
            AdvancedMogreFramework.Singleton.m_pMouse.MousePressed -= mousePressed;
            AdvancedMogreFramework.Singleton.m_pMouse.MouseReleased -= mouseReleased;
            AdvancedMogreFramework.Singleton.m_pKeyboard.KeyPressed -= keyPressed;
            AdvancedMogreFramework.Singleton.m_pKeyboard.KeyReleased -= keyReleased;

            if (m_pSceneMgr!=null)
                m_pSceneMgr.DestroyCamera(m_pCamera);
                m_pSceneMgr.DestroyQuery(m_pRSQ);
                AdvancedMogreFramework.Singleton.m_pRoot.DestroySceneManager(m_pSceneMgr);
        }
        public override bool pause()
        {
            AdvancedMogreFramework.Singleton.m_pLog.LogMessage("Pausing GameState...");
 
            return true;
        }
        public override void resume()
        {
            AdvancedMogreFramework.Singleton.m_pLog.LogMessage("Resuming GameState...");
 
            buildGUI();

            AdvancedMogreFramework.Singleton.m_pViewport.Camera=m_pCamera;
            m_bQuit = false;
        }
 
	    public void moveCamera()
        {
            if (m_pCamera != null)
            {
                if (AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_LSHIFT))
                    m_pCamera.MoveRelative(m_TranslateVector);
                m_pCamera.MoveRelative(m_TranslateVector / 10);
            }
        }
        public void getInput()
        {
            if(m_bSettingsMode == false)
            {
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_A))
                    m_TranslateVector.x = -10;
 
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_D))
                    m_TranslateVector.x = 10;
 
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_W))
                    m_TranslateVector.z = -10;
 
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_S))
                    m_TranslateVector.z = 10;
 
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_Q))
                    m_TranslateVector.y = -10;
 
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_E))
                    m_TranslateVector.y = 10;
 
        //camera roll
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_Z))
                    m_pCamera.Roll(new Angle(-10));
 
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_X))
                    m_pCamera.Roll(new Angle(10));
 
        //reset roll
                if (AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_C))
                    m_pCamera.Roll(-(m_pCamera.RealOrientation.Roll));
            }
        }
        public void buildGUI()
        {
            AdvancedMogreFramework.Singleton.m_pTrayMgr.showFrameStats(TrayLocation.TL_BOTTOMLEFT);
            AdvancedMogreFramework.Singleton.m_pTrayMgr.showLogo(TrayLocation.TL_BOTTOMRIGHT);
            AdvancedMogreFramework.Singleton.m_pTrayMgr.createLabel(TrayLocation.TL_TOP, "GameLbl", "Game mode", 250);
            AdvancedMogreFramework.Singleton.m_pTrayMgr.showCursor();
 
            List<string> items=new List<string>();
            items.Insert(items.Count,"cam.pX");
            items.Insert(items.Count,"cam.pY");
            items.Insert(items.Count,"cam.pZ");
            items.Insert(items.Count,"cam.oW");
            items.Insert(items.Count,"cam.oX");
            items.Insert(items.Count,"cam.oY");
            items.Insert(items.Count,"cam.oZ");
            items.Insert(items.Count,"Mode");

            m_pDetailsPanel = AdvancedMogreFramework.Singleton.m_pTrayMgr.createParamsPanel(TrayLocation.TL_TOPLEFT, "DetailsPanel", 200, items.ToArray());
            m_pDetailsPanel.show();
 
            string infoText = "[TAB] - Switch input mode\n\n[W] - Forward / Mode up\n[S] - Backwards/ Mode down\n[A] - Left\n";
            infoText.Insert(infoText.Length,"[D] - Right\n\nPress [SHIFT] to move faster\n\n[O] - Toggle FPS / logo\n");
            infoText.Insert(infoText.Length,"[Print] - Take screenshot\n\n[ESC] - Exit");
            AdvancedMogreFramework.Singleton.m_pTrayMgr.createTextBox(TrayLocation.TL_RIGHT, "InfoPanel", infoText, 300, 220);
 
            StringVector chatModes=new StringVector();
            chatModes.Insert(chatModes.Count,"Solid mode");
            chatModes.Insert(chatModes.Count,"Wireframe mode");
            chatModes.Insert(chatModes.Count,"Point mode");
            AdvancedMogreFramework.Singleton.m_pTrayMgr.createLongSelectMenu(TrayLocation.TL_TOPRIGHT, "ChatModeSelMenu", "ChatMode", 200, 3, chatModes);

            AdvancedMogreFramework.Singleton.m_pMouse.MouseMoved += mouseMoved;
            AdvancedMogreFramework.Singleton.m_pMouse.MousePressed += mousePressed;
            AdvancedMogreFramework.Singleton.m_pMouse.MouseReleased += mouseReleased;
            AdvancedMogreFramework.Singleton.m_pKeyboard.KeyPressed += keyPressed;
            AdvancedMogreFramework.Singleton.m_pKeyboard.KeyReleased += keyReleased;
        }

        public bool keyPressed(KeyEvent keyEventRef)
        {
            if(m_bSettingsMode == true)
            {
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_S))
                {
                    SelectMenu pMenu = (SelectMenu)AdvancedMogreFramework.Singleton.m_pTrayMgr.getWidget("ChatModeSelMenu");
                    if(pMenu.getSelectionIndex() + 1 < (int)pMenu.getNumItems())
                        pMenu.selectItem((uint)pMenu.getSelectionIndex() + 1);
                }
 
                if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_W))
                {
                    SelectMenu pMenu = (SelectMenu)AdvancedMogreFramework.Singleton.m_pTrayMgr.getWidget("ChatModeSelMenu");
                    if(pMenu.getSelectionIndex() - 1 >= 0)
                        pMenu.selectItem((uint)pMenu.getSelectionIndex() - 1);
                }
             }
 
            if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_ESCAPE))
            {
                pushAppState(findByName("PauseState"));
                return true;
            }
 
            if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_I))
            {
                if(m_pDetailsPanel.getTrayLocation() == TrayLocation.TL_NONE)
                {
                    AdvancedMogreFramework.Singleton.m_pTrayMgr.moveWidgetToTray(m_pDetailsPanel, TrayLocation.TL_TOPLEFT, 0);
                    m_pDetailsPanel.show();
                }
                else
                {
                    AdvancedMogreFramework.Singleton.m_pTrayMgr.removeWidgetFromTray(m_pDetailsPanel);
                    m_pDetailsPanel.hide();
                }
            }
 
            if(AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_TAB))
            {
                m_bSettingsMode = !m_bSettingsMode;
                return true;
            }
 
            if(m_bSettingsMode && AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_RETURN) ||
                AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_NUMPADENTER))
            {
            }
 
            if(!m_bSettingsMode || (m_bSettingsMode && !AdvancedMogreFramework.Singleton.m_pKeyboard.IsKeyDown(KeyCode.KC_O)))
                AdvancedMogreFramework.Singleton.keyPressed(keyEventRef);
 
                return true;
        }
        public bool keyReleased(KeyEvent keyEventRef)
        {
            AdvancedMogreFramework.Singleton.keyPressed(keyEventRef);
            return true;
        }

        public bool mouseMoved(MouseEvent evt)
        {
            if (AdvancedMogreFramework.Singleton.m_pTrayMgr.injectMouseMove(evt)) return true;
 
            if(m_bRMouseDown)
            {
                Degree deCameraYaw = new Degree(evt.state.X.rel * -0.1f);
                m_pCamera.Yaw(deCameraYaw);
                Degree deCameraPitch = new Degree(evt.state.Y.rel * -0.1f);
                m_pCamera.Pitch(deCameraPitch);
            }
 
            return true;
        }
        public bool mousePressed(MouseEvent evt, MouseButtonID id)
        {
            if (AdvancedMogreFramework.Singleton.m_pTrayMgr.injectMouseDown(evt, id)) return true;
 
            if(id == MouseButtonID.MB_Left)
            {
                onLeftPressed(evt);
                m_bLMouseDown = true;
            }
            else if (id == MouseButtonID.MB_Right)
            {
                m_bRMouseDown = true;
            }
 
            return true;
        }
	    public bool mouseReleased(MouseEvent evt, MouseButtonID id)
        {
            if (AdvancedMogreFramework.Singleton.m_pTrayMgr.injectMouseUp(evt, id)) return true;
 
            if(id == MouseButtonID.MB_Left)
            {
                m_bLMouseDown = false;
            }
            else if(id == MouseButtonID.MB_Right)
            {
                m_bRMouseDown = false;
            }
 
            return true;
        }

        public void onLeftPressed(MouseEvent evt)
        {
            if(m_pCurrentObject!=null)
            {
                m_pCurrentObject.ShowBoundingBox=false;
                m_pCurrentEntity.GetSubEntity(1).SetMaterial(m_pOgreHeadMat);
            }
 
            Ray mouseRay = m_pCamera.GetCameraToViewportRay(AdvancedMogreFramework.Singleton.m_pMouse.MouseState.X.abs / (float)evt.state.width,
            AdvancedMogreFramework.Singleton.m_pMouse.MouseState.Y.abs / (float)evt.state.height);
            m_pRSQ.Ray=mouseRay;
            //m_pRSQ.SortByDistance=true;
 
            RaySceneQueryResult result = m_pRSQ.Execute();
 
            foreach(RaySceneQueryResultEntry itr in result)
            {
                if(itr.movable!=null)
                {
                    AdvancedMogreFramework.Singleton.m_pLog.LogMessage("MovableName: " + itr.movable.Name);
                    m_pCurrentObject = m_pSceneMgr.GetEntity(itr.movable.Name).ParentSceneNode;
                    AdvancedMogreFramework.Singleton.m_pLog.LogMessage("ObjName " + m_pCurrentObject.Name);
                    m_pCurrentObject.ShowBoundingBox=true;
                    m_pCurrentEntity = m_pSceneMgr.GetEntity(itr.movable.Name);
                    m_pCurrentEntity.GetSubEntity(1).SetMaterial(m_pOgreHeadMatHigh);
                    break;
                }
            }
        }
        public override void itemSelected(SelectMenu menu)
        {
            switch(menu.getSelectionIndex())
            {
            case 0:
                m_pCamera.PolygonMode=(PolygonMode.PM_SOLID);break;
            case 1:
                m_pCamera.PolygonMode=(PolygonMode.PM_WIREFRAME);break;
            case 2:
                m_pCamera.PolygonMode=(PolygonMode.PM_POINTS);break;
    }
        }

        public override void update(double timeSinceLastFrame)
        {
            m_FrameEvent.timeSinceLastFrame = (float)timeSinceLastFrame;
            if (AdvancedMogreFramework.Singleton.m_pTrayMgr != null)
            {
                AdvancedMogreFramework.Singleton.m_pTrayMgr.frameRenderingQueued(m_FrameEvent);
            }
 
            if(m_bQuit == true)
            {
                popAppState();
                return;
            }
            if (AdvancedMogreFramework.Singleton.m_pTrayMgr != null)
            {
                if (!AdvancedMogreFramework.Singleton.m_pTrayMgr.isDialogVisible())
                {
                    if (m_pDetailsPanel!=null && m_pDetailsPanel.isVisible())
                    {
                        m_pDetailsPanel.setParamValue(0, m_pCamera.DerivedPosition.x.ToString());
                        m_pDetailsPanel.setParamValue(1, m_pCamera.DerivedPosition.y.ToString());
                        m_pDetailsPanel.setParamValue(2, m_pCamera.DerivedPosition.z.ToString());
                        m_pDetailsPanel.setParamValue(3, m_pCamera.DerivedOrientation.w.ToString());
                        m_pDetailsPanel.setParamValue(4, m_pCamera.DerivedOrientation.x.ToString());
                        m_pDetailsPanel.setParamValue(5, m_pCamera.DerivedOrientation.y.ToString());
                        m_pDetailsPanel.setParamValue(6, m_pCamera.DerivedOrientation.z.ToString());
                        if (m_bSettingsMode)
                            m_pDetailsPanel.setParamValue(7, "Buffered Input");
                        else
                            m_pDetailsPanel.setParamValue(7, "Un-Buffered Input");
                    }
                }
            }
 
            m_MoveScale = m_MoveSpeed   * (float)timeSinceLastFrame;
            m_RotScale  = m_RotateSpeed * (float)timeSinceLastFrame;
 
            m_TranslateVector = Mogre.Vector3.ZERO;
 
            getInput();
            moveCamera();
        }
        SceneNode m_pOgreHeadNode;
        Entity m_pOgreHeadEntity;
        MaterialPtr m_pOgreHeadMat;
        MaterialPtr m_pOgreHeadMatHigh;

        ParamsPanel m_pDetailsPanel;
        bool m_bQuit;

        Mogre.Vector3 m_TranslateVector;
        float m_MoveSpeed;
        Degree m_RotateSpeed;
        float m_MoveScale;
        Degree m_RotScale;

        RaySceneQuery m_pRSQ;
        SceneNode m_pCurrentObject;
        Entity m_pCurrentEntity;
        bool m_bRMouseDown,m_bLMouseDown;
        bool m_bSettingsMode;
    }
}
