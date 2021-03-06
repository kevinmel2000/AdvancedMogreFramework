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
using AdvancedMogreFramework.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Mogre;
using MOIS;
using Mogre_Procedural.MogreBites;

namespace AdvancedMogreFramework.States
{
    class AppStateManager : AppStateListener,IDisposable
    {
        bool disposed;

         public struct state_info
        {
            public String name;
            public AppState state;
        };
         public AppStateManager()
         {
             m_bShutdown = false;
         }

          public override void manageAppState(String stateName, AppState state)
         {
		    state_info new_state_info;
		    new_state_info.name = stateName;
		    new_state_info.state = state;
		    m_States.Add(new_state_info);
         }

         public override AppState findByName(String stateName)
         {
            foreach (state_info itr in m_States)
	        {
		        if(itr.name==stateName)
			    return itr.state;
	        }
 
	        return null;
         }

         public void start(AppState state)
         {
            changeAppState(state);
 
	        int timeSinceLastFrame = 1;
	        int startTime = 0;
 
	        while(!m_bShutdown)
	        {
		        if(AdvancedMogreFramework.Singleton.m_pRenderWnd.IsClosed)m_bShutdown = true;
 
		        WindowEventUtilities.MessagePump();

                if (AdvancedMogreFramework.Singleton.m_pRenderWnd.IsActive)
		        {
                    startTime = (int)AdvancedMogreFramework.Singleton.m_pTimer.MicrosecondsCPU;

                    AdvancedMogreFramework.Singleton.m_pKeyboard.Capture();
                    AdvancedMogreFramework.Singleton.m_pMouse.Capture();

                    m_ActiveStateStack.Last().update(timeSinceLastFrame * 1.0 / 1000);
                    AdvancedMogreFramework.Singleton.m_pKeyboard.Capture();
                    AdvancedMogreFramework.Singleton.m_pMouse.Capture();
                    AdvancedMogreFramework.Singleton.updateOgre(timeSinceLastFrame * 1.0 / 1000);
                    if (AdvancedMogreFramework.Singleton.m_pRoot != null)
                    {
                        AdvancedMogreFramework.Singleton.m_pRoot.RenderOneFrame();
                    }
                    timeSinceLastFrame = (int)AdvancedMogreFramework.Singleton.m_pTimer.MicrosecondsCPU - startTime;
		        }
		        else
		        {
                    System.Threading.Thread.Sleep(1000);
		        }
	        }

            AdvancedMogreFramework.Singleton.m_pLog.LogMessage("Main loop quit");
         }
         public override void changeAppState(AppState state)
         {
             if (m_ActiveStateStack.Count!=0)
             {
                 m_ActiveStateStack.Last().exit();
                 m_ActiveStateStack.RemoveAt(m_ActiveStateStack.Count()-1);
             }

             m_ActiveStateStack.Add(state);
             init(state);
             m_ActiveStateStack.Last().enter();
         }
         public override bool pushAppState(AppState state)
         {
             if (m_ActiveStateStack.Count!=0)
             {
                 if (!m_ActiveStateStack.Last().pause())
                     return false;
             }

             m_ActiveStateStack.Add(state);
             init(state);
             m_ActiveStateStack.Last().enter();

             return true;
         }
         public override void popAppState()
         {
             if (m_ActiveStateStack.Count != 0)
             {
                 m_ActiveStateStack.Last().exit();
                 m_ActiveStateStack.RemoveAt(m_ActiveStateStack.Count()-1);
             }

             if (m_ActiveStateStack.Count != 0)
             {
                 init(m_ActiveStateStack.Last());
                 m_ActiveStateStack.Last().resume();
             }
             else
                 shutdown();
         }
         public override void popAllAndPushAppState<T>(AppState state)
        {
            while (m_ActiveStateStack.Count != 0)
            {
                m_ActiveStateStack.Last().exit();
                m_ActiveStateStack.RemoveAt(m_ActiveStateStack.Count()-1);
            }

            pushAppState(state);
        }
         public override void pauseAppState()
         {
             if (m_ActiveStateStack.Count != 0)
             {
                 m_ActiveStateStack.Last().pause();
             }

             if (m_ActiveStateStack.Count() > 2)
             {
                 init(m_ActiveStateStack.ElementAt(m_ActiveStateStack.Count() - 2));
                 m_ActiveStateStack.ElementAt(m_ActiveStateStack.Count() - 2).resume();
             }
         }
         public override void shutdown()
         {
             m_bShutdown = true;
         }

         protected void init(AppState state)
         {
             AdvancedMogreFramework.Singleton.m_pTrayMgr.setListener(state);
             AdvancedMogreFramework.Singleton.m_pRenderWnd.ResetStatistics();
         }

         protected List<AppState> m_ActiveStateStack=new List<AppState>();
         protected List<state_info> m_States=new List<state_info>();
         protected bool m_bShutdown;

         public void Dispose()
         {
             Dispose(true);
             GC.SuppressFinalize(this);
         }

         protected virtual void Dispose(bool disposing)
         {
             if (disposed)
             {
                 return;
             }
             if (disposing)
             {

                 state_info si;

                 while (m_ActiveStateStack.Count != 0)
                 {
                     m_ActiveStateStack.Last().exit();
                     m_ActiveStateStack.RemoveAt(m_ActiveStateStack.Count() - 1);
                 }

                 while (m_States.Count != 0)
                 {
                     si = m_States.Last();
                     si.state.destroy();
                     m_States.RemoveAt(m_States.Count() - 1);
                 }
             }
             disposed = true;
         }
    }
}
