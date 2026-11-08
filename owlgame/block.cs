using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;

namespace owlgame
{
    class block
    {
        public block(int x,int y) 
        {
            curx = x;
            cury = y;
            int no = NextShape();
            // Classic Tetris rule: a shape always gets the same colour, so the
            // colour is derived from the shape index instead of a separate roll.
            colorblock = boxcolors[no];
            for (int i = 0; i < size; ++i)
                for (int j = 0; j < size; ++j)
                    matblock[i, j] = blockbox[no, i, j];
        }

        /// <summary>
        /// Copy constructor: duplicates shape / colour / position without
        /// touching the random source. The rotation code used to create a
        /// throw-away block just to hold a rotated matrix, which silently
        /// consumed random values on every key press.
        /// </summary>
        public block(block src)
        {
            curx = src.curx;
            cury = src.cury;
            colorblock = src.colorblock;
            for (int i = 0; i < size; ++i)
                for (int j = 0; j < size; ++j)
                    matblock[i, j] = src.matblock[i, j];
        }

        /// <summary>
        /// Draw the next shape index from the shared generator.
        /// A per-instance "new Random()" seeds from Environment.TickCount
        /// (about 15 ms resolution), so two blocks created back to back got
        /// identical shapes and colours - the "next" preview used to show a
        /// copy of the current block on every new game.
        /// </summary>
        private static int NextShape()
        {
            lock (randerLock)
                return rander.Next(No);
        }
        
        public int[,] matblock = new int[size, size];         // 盒子矩阵
        public Color colorblock;
        public int curx,cury;                                 // 盒子的起点
        public static int size = 5;                           // 盒子矩阵的维数
        private const int No = 7;                             // 盒子的种数        
        // One shared generator for the whole game, guarded by a lock
        // because Random itself is not thread safe.
        private static readonly Random rander = new Random();
        private static readonly object randerLock = new object();
        private static Color[] boxcolors ={                   // 预定义盒子颜色
            Color.Cyan, Color.Magenta, Color.Lime, Color.Orange,
            Color.Blue, Color.Yellow, Color.Red
        }; 
        private static int[,,] blockbox ={                     // 预定义的盒子
            {
                {0,0,0,0,0},
                {0,0,0,0,0},
                {1,1,1,1,0},
                {0,0,0,0,0},
                {0,0,0,0,0}
            },
            
            {
                {0,0,0,0,0},
                {0,0,1,0,0},
                {0,1,1,1,0},
                {0,0,0,0,0},
                {0,0,0,0,0}
            },
            
            {
                {0,0,0,0,0},
                {0,1,1,0,0},
                {0,0,1,1,0},
                {0,0,0,0,0},
                {0,0,0,0,0}
            },
            
            {
                {0,0,0,0,0},
                {0,0,1,1,0},
                {0,1,1,0,0},
                {0,0,0,0,0},
                {0,0,0,0,0}
            },
            
            {
                {0,0,0,0,0},
                {0,1,1,0,0},
                {0,0,1,0,0},
                {0,0,1,0,0},
                {0,0,0,0,0}
            },
            
            {
                {0,0,0,0,0},
                {0,0,1,1,0},
                {0,0,1,0,0},
                {0,0,1,0,0},
                {0,0,0,0,0}
            },
            
            {
                {0,0,0,0,0},
                {0,0,1,1,0},
                {0,0,1,1,0},
                {0,0,0,0,0},
                {0,0,0,0,0}
            }
         };
    }
}
