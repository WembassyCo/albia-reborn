using System;
using System.Collections.Generic;
using Albia.Core.Shared;
using UnityEngine;

namespace Albia.Pods.Terrain
{
    /// <summary>
    /// Implements the classic Marching Cubes algorithm for voxel mesh generation.
    /// Converts scalar density fields into triangle meshes.
    /// </summary>
    public static class MarchingCubes
    {
        // ISO level for surface extraction (0 = surface between solid and empty)
        public const float IsoLevel = 0f;
        
        // Chunk dimensions + padding for edge cases
        private const int PaddedSize = VoxelData.SIZE + 1;

        // ============================================
        // EDGE TABLE - Determines which edges are intersected
        // 256 entries, 12 bits each (one per edge)
        // ============================================
        private static readonly int[] EdgeTable = new int[256]
        {
            0x000, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
            0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
            0x190, 0x099, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
            0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
            0x230, 0x339, 0x033, 0x13a, 0x636, 0x73f, 0x435, 0x53c,
            0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
            0x3a0, 0x2a9, 0x1a3, 0x0aa, 0x7a6, 0x6af, 0x5a5, 0x4ac,
            0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
            0x460, 0x569, 0x663, 0x76a, 0x066, 0x16f, 0x265, 0x36c,
            0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
            0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0x0ff, 0x3f5, 0x2fc,
            0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
            0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x055, 0x15c,
            0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
            0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0x0cc,
            0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
            0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
            0x0cc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
            0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
            0x15c, 0x055, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
            0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
            0x2fc, 0x3f5, 0x0ff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
            0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
            0x36c, 0x265, 0x16f, 0x066, 0x76a, 0x663, 0x569, 0x460,
            0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
            0x4ac, 0x5a5, 0x6af, 0x7a6, 0x0aa, 0x1a3, 0x2a9, 0x3a0,
            0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
            0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x033, 0x339, 0x230,
            0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
            0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x099, 0x190,
            0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
            0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x000
        };

        // ============================================
        // TRIANGLE TABLE - Lists vertex indices for each case
        // 256 cases, up to 5 triangles (15 vertices) each
        // -1 = end of triangle list
        // ============================================
        private static readonly int[][] TriTable = new int[256][]
        {
            new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[] {3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[] {3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
            new int[] {8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[] {11, 4, 7, 11, 2, 4, 9, 2, 1, 9, 4, 2, -1, -1, -1, -1},
            new int[] {3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
            new int[] {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
            new int[] {4, 7, 11, 9, 4, 11, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
            new int[] {9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
            new int[] {10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
            new int[] {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
            new int[] {5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[] {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
            new int[] {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
            new int[] {2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[] {7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
            new int[] {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
            new int[] {11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
            new int[] {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
            new int[] {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
            new int[] {11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[] {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
            new int[] {2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[] {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
            new int[] {6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
            new int[] {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
            new int[] {6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[] {5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[] {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
            new int[] {6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
            new int[] {8, 4, 7, 9, 2, 0, 9, 6, 2, 9, 5, 6, -1, -1, -1, -1},
            new int[] {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
            new int[] {3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
            new int[] {0, 1, 9, 2, 3, 11, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1},
            new int[] {2, 1, 9, 2, 9, 11, 9, 4, 7, 11, 9, 7, 6, 5, 10, -1},
            new int[] {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
            new int[] {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
            new int[] {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 4, 7, 8, -1},
            new int[] {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
            new int[] {4, 5, 9, 7, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {3, 0, 8, 4, 5, 9, 7, 5, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 5, 9, 0, 6, 5, 0, 3, 6, 7, 11, 6, -1, -1, -1, -1},
            new int[] {3, 9, 8, 3, 6, 9, 3, 5, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 5, 9, 4, 7, 5, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {9, 4, 7, 9, 5, 4, 10, 2, 1, 11, 10, 1, -1, -1, -1, -1},
            new int[] {6, 3, 0, 6, 5, 9, 7, 5, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[] {6, 3, 0, 6, 9, 3, 9, 1, 3, 4, 9, 7, 10, 2, 11, -1},
            new int[] {5, 2, 10, 5, 4, 7, 5, 9, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 7, 11, 2, 4, 7, 11, 10, 2, 10, 4, 2, -1, -1, -1, -1},
            new int[] {9, 4, 7, 9, 7, 11, 9, 11, 3, 10, 5, 2, -1, -1, -1, -1},
            new int[] {5, 2, 10, 5, 4, 7, 2, 4, 10, 2, 0, 4, -1, -1, -1, -1},
            new int[] {4, 8, 5, 5, 8, 3, 5, 3, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[] {3, 11, 5, 3, 5, 4, 3, 4, 0, 5, 11, 6, -1, -1, -1, -1},
            new int[] {4, 8, 0, 4, 0, 9, 5, 6, 11, 10, 5, 11, -1, -1, -1, -1},
            new int[] {5, 6, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 10, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {1, 6, 10, 1, 5, 6, 1, 9, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 2, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 2, 4, 2, 0, 1, 5, 3, 5, 7, 3, -1, -1, -1, -1},
            new int[] {9, 4, 6, 9, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 10, 6, 2, 6, 9, 9, 6, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 10, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {0, 11, 2, 0, 8, 11, 4, 6, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {10, 4, 6, 10, 9, 4, 1, 0, 2, 11, 2, 3, -1, -1, -1, -1},
            new int[] {1, 4, 9, 1, 10, 6, 2, 4, 1, 4, 6, 1, -1, -1, -1, -1},
            new int[] {5, 11, 6, 5, 10, 11, 10, 3, 11, 10, 1, 3, -1, -1, -1, -1},
            new int[] {0, 10, 1, 0, 6, 10, 0, 4, 6, 11, 6, 10, -1, -1, -1, -1},
            new int[] {11, 6, 10, 11, 10, 3, 3, 10, 9, 9, 4, 3, -1, -1, -1, -1},
            new int[] {10, 4, 9, 10, 6, 4, 11, 6, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 7, 4, 7, 10, 10, 7, 8, 10, 8, 3, 11, 3, 8, -1},
            new int[] {4, 6, 7, 1, 4, 6, 1, 6, 10, 1, 10, 2, 11, 2, 10, -1},
            new int[] {0, 4, 6, 10, 7, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {10, 2, 11, 10, 11, 6, 11, 7, 6, 4, 9, 6, -1, -1, -1, -1},
            new int[] {1, 0, 10, 10, 0, 6, 6, 0, 4, 6, 4, 7, -1, -1, -1, -1},
            new int[] {4, 6, 7, 4, 9, 6, 9, 11, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 7, 4, 11, 6, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1},
            new int[] {5, 8, 4, 5, 11, 8, 10, 4, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 11, 8, 4, 6, 11, 9, 6, 5, 10, 4, 5, -1, -1, -1, -1},
            new int[] {3, 0, 5, 3, 5, 11, 3, 11, 10, 10, 4, 5, -1, -1, -1, -1},
            new int[] {10, 4, 5, 11, 10, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {5, 8, 4, 5, 11, 8, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 1, 9, 4, 11, 1, 11, 10, 1, 11, 8, 10, -1, -1, -1, -1},
            new int[] {6, 0, 8, 6, 8, 11, 6, 11, 10, 4, 10, 5, -1, -1, -1, -1},
            new int[] {5, 4, 11, 9, 10, 6, 6, 11, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 11, 8, 4, 6, 11, 0, 2, 9, 10, 6, 11, -1, -1, -1, -1},
            new int[] {10, 2, 1, 10, 1, 9, 9, 1, 0, 9, 0, 6, 6, 0, 8, -1},
            new int[] {4, 11, 8, 4, 6, 11, 9, 5, 10, 9, 10, 11, -1, -1, -1, -1},
            new int[] {0, 4, 11, 0, 11, 2, 2, 11, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[] {4, 6, 11, 0, 4, 11, 2, 0, 9, 2, 11, 10, -1, -1, -1, -1},
            new int[] {5, 11, 8, 5, 6, 11, 9, 4, 11, 9, 11, 10, -1, -1, -1, -1},
            new int[] {0, 6, 8, 0, 2, 6, 2, 11, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 11, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[] {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
            new int[] {2, 11, 3, 2, 6, 11, 2, 0, 6, 4, 6, 9, 9, 0, 6, -1},
            new int[] {0, 3, 11, 0, 11, 6, 0, 6, 9, 7, 6, 11, -1, -1, -1, -1},
            new int[] {6, 11, 3, 6, 3, 5, 6, 5, 9, 4, 5, 3, -1, -1, -1, -1},
            new int[] {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1}
        };

        // Edge vertex offsets (12 edges of a cube)
        private static readonly Vector3[] EdgeOffsets = new Vector3[12]
        {
            new Vector3(0.5f, 0, 0),    // Edge 0: (0,0,0) to (1,0,0)
            new Vector3(1, 0.5f, 0),    // Edge 1: (1,0,0) to (1,1,0)
            new Vector3(0.5f, 1, 0),    // Edge 2: (1,1,0) to (0,1,0)
            new Vector3(0, 0.5f, 0),    // Edge 3: (0,1,0) to (0,0,0)
            new Vector3(0.5f, 0, 1),    // Edge 4: (0,0,1) to (1,0,1)
            new Vector3(1, 0.5f, 1),    // Edge 5: (1,0,1) to (1,1,1)
            new Vector3(0.5f, 1, 1),    // Edge 6: (1,1,1) to (0,1,1)
            new Vector3(0, 0.5f, 1),    // Edge 7: (0,1,1) to (0,0,1)
            new Vector3(0, 0, 0.5f),    // Edge 8: (0,0,0) to (0,0,1)
            new Vector3(1, 0, 0.5f),    // Edge 9: (1,0,0) to (1,0,1)
            new Vector3(1, 1, 0.5f),    // Edge 10: (1,1,0) to (1,1,1)
            new Vector3(0, 1, 0.5f)     // Edge 11: (0,1,0) to (0,1,1)
        };

        // Corner offsets for cube vertices
        private static readonly Vector3[] CornerOffsets = new Vector3[8]
        {
            new Vector3(0, 0, 0),  // 0
            new Vector3(1, 0, 0),  // 1
            new Vector3(1, 1, 0),  // 2
            new Vector3(0, 1, 0),  // 3
            new Vector3(0, 0, 1),  // 4
            new Vector3(1, 0, 1),  // 5
            new Vector3(1, 1, 1),  // 6
            new Vector3(0, 1, 1)   // 7
        };

        /// <summary>
        /// Generates mesh data from a density field using Marching Cubes
        /// </summary>
        /// <param name="densities">Float array of size (SIZE+1)^3 with density values</param>
        /// <param name="lod">Level of Detail (0 = full, 1 = half, etc.)</param>
        /// <returns>Mesh data containing vertices, normals, and triangle indices</returns>
        public static MeshData GenerateMesh(float[] densities, int lod = 0)
        {
            int step = 1 << lod;  // Step size based on LOD (1, 2, 4, 8...)
            int size = VoxelData.SIZE / step;
            
            var meshData = new MeshData();
            var vertexCache = new Dictionary<int, int>(256);

            // Iterate through cubes (cells)
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Get densities at 8 corners of this cube
                        float[] cornerDensities = GetCornerDensities(densities, x * step, y * step, z * step, step);
                        
                        // Calculate cube index (0-255)
                        int cubeIndex = CalculateCubeIndex(cornerDensities);
                        
                        if (cubeIndex == 0 || cubeIndex == 255)
                            continue;  // Fully inside or fully outside

                        // Generate triangles for this cube
                        ProcessCube(x * step, y * step, z * step, step, cornerDensities, cubeIndex, meshData, vertexCache);
                    }
                }
            }

            return meshData;
        }

        /// <summary>
        /// Gets density values at 8 corners of a cube
        /// </summary>
        private static float[] GetCornerDensities(float[] densities, int x, int y, int z, int step)
        {
            float[] corners = new float[8];
            int size = VoxelData.SIZE + 1;
            
            for (int i = 0; i < 8; i++)
            {
                int cx = x + (int)(CornerOffsets[i].x * step);
                int cy = y + (int)(CornerOffsets[i].y * step);
                int cz = z + (int)(CornerOffsets[i].z * step);
                
                // Clamp to valid range
                cx = Mathf.Clamp(cx, 0, VoxelData.SIZE);
                cy = Mathf.Clamp(cy, 0, VoxelData.SIZE);
                cz = Mathf.Clamp(cz, 0, VoxelData.SIZE);
                
                corners[i] = densities[cy * size * size + cz * size + cx];
            }
            
            return corners;
        }

        /// <summary>
        /// Calculates the cube index (0-255) based on which corners are inside
        /// </summary>
        private static int CalculateCubeIndex(float[] cornerDensities)
        {
            int index = 0;
            for (int i = 0; i < 8; i++)
            {
                if (cornerDensities[i] < IsoLevel)
                    index |= (1 << i);
            }
            return index;
        }

        /// <summary>
        /// Processes a single cube, generating triangles
        /// </summary>
        private static void ProcessCube(int x, int y, int z, int step, float[] cornerDensities, int cubeIndex, 
            MeshData meshData, Dictionary<int, int> vertexCache)
        {
            int edgeFlags = EdgeTable[cubeIndex];
            
            if (edgeFlags == 0)
                return;

            // Calculate intersection points on edges
            Vector3[] edgeVertices = new Vector3[12];
            for (int i = 0; i < 12; i++)
            {
                if ((edgeFlags & (1 << i)) != 0)
                {
                    edgeVertices[i] = InterpolateEdge(x, y, z, step, i, cornerDensities);
                }
            }

            // Create triangles
            int[] triIndices = TriTable[cubeIndex];
            
            for (int i = 0; i < triIndices.Length && triIndices[i] != -1; i += 3)
            {
                // Get or create vertices
                int v0 = GetOrCreateVertex(x, y, z, triIndices[i], edgeVertices, meshData, vertexCache);
                int v1 = GetOrCreateVertex(x, y, z, triIndices[i + 1], edgeVertices, meshData, vertexCache);
                int v2 = GetOrCreateVertex(x, y, z, triIndices[i + 2], edgeVertices, meshData, vertexCache);
                
                // Add triangle (wind counter-clockwise)
                meshData.Triangles.Add(v0);
                meshData.Triangles.Add(v1);
                meshData.Triangles.Add(v2);
            }
        }

        /// <summary>
        /// Gets or creates a vertex for an edge intersection
        /// </summary>
        private static int GetOrCreateVertex(int cx, int cy, int cz, int edgeIndex, Vector3[] edgeVertices, 
            MeshData meshData, Dictionary<int, int> vertexCache)
        {
            // Create unique key for this edge
            int key = (cx << 20) | (cy << 10) | (cz << 4) | edgeIndex;
            
            if (vertexCache.TryGetValue(key, out int vertexIndex))
                return vertexIndex;

            // Create new vertex
            vertexIndex = meshData.Vertices.Count;
            vertexCache[key] = vertexIndex;
            
            Vector3 pos = edgeVertices[edgeIndex];
            meshData.Vertices.Add(pos);
            
            // Calculate normal (simplified - using gradient)
            Vector3 normal = CalculateNormal(pos);
            meshData.Normals.Add(normal);
            
            // Calculate UV based on position
            Vector2 uv = new Vector2(pos.x % 1f, pos.z % 1f);
            meshData.UVs.Add(uv);
            
            return vertexIndex;
        }

        /// <summary>
        /// Interpolates the position along an edge based on density values
        /// </summary>
        private static Vector3 InterpolateEdge(int x, int y, int z, int step, int edgeIndex, float[] cornerDensities)
        {
            // Get the two corners that this edge connects
            int[] edgeCorners = GetEdgeCorners(edgeIndex);
            int i1 = edgeCorners[0];
            int i2 = edgeCorners[1];
            
            float d1 = cornerDensities[i1];
            float d2 = cornerDensities[i2];
            
            // Interpolation factor (0 to 1)
            float t = d1 == d2 ? 0.5f : (IsoLevel - d1) / (d2 - d1);
            t = Mathf.Clamp01(t);
            
            // Local positions of corners
            Vector3 p1 = CornerOffsets[i1] * step;
            Vector3 p2 = CornerOffsets[i2] * step;
            
            // Interpolated position
            Vector3 localPos = Vector3.Lerp(p1, p2, t);
            
            // World position within chunk
            return new Vector3(x + localPos.x, y + localPos.y, z + localPos.z);
        }

        /// <summary>
        /// Gets the corner indices for each edge
        /// </summary>
        private static int[] GetEdgeCorners(int edgeIndex)
        {
            switch (edgeIndex)
            {
                case 0: return new[] { 0, 1 };
                case 1: return new[] { 1, 2 };
                case 2: return new[] { 2, 3 };
                case 3: return new[] { 3, 0 };
                case 4: return new[] { 4, 5 };
                case 5: return new[] { 5, 6 };
                case 6: return new[] { 6, 7 };
                case 7: return new[] { 7, 4 };
                case 8: return new[] { 0, 4 };
                case 9: return new[] { 1, 5 };
                case 10: return new[] { 2, 6 };
                case 11: return new[] { 3, 7 };
                default: return new[] { 0, 1 };
            }
        }

        /// <summary>
        /// Calculates surface normal at a point (simplified)
        /// </summary>
        private static Vector3 CalculateNormal(Vector3 pos)
        {
            // For now, return a smoothed normal based on position
            // In a full implementation, this would sample neighboring densities
            return Vector3.up;
        }

        /// <summary>
        /// Gets interpolated density at any position within the chunk
        /// </summary>
        public static float SampleDensity(float[] densities, float x, float y, float z)
        {
            int size = VoxelData.SIZE + 1;
            
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            int iz = Mathf.FloorToInt(z);
            
            float fx = x - ix;
            float fy = y - iy;
            float fz = z - iz;
            
            // Clamp
            ix = Mathf.Clamp(ix, 0, VoxelData.SIZE - 1);
            iy = Mathf.Clamp(iy, 0, VoxelData.SIZE - 1);
            iz = Mathf.Clamp(iz, 0, VoxelData.SIZE - 1);
            
            // Trilinear interpolation
            float d000 = densities[iy * size * size + iz * size + ix];
            float d100 = densities[iy * size * size + iz * size + (ix + 1)];
            float d010 = densities[(iy + 1) * size * size + iz * size + ix];
            float d110 = densities[(iy + 1) * size * size + iz * size + (ix + 1)];
            float d001 = densities[iy * size * size + (iz + 1) * size + ix];
            float d101 = densities[iy * size * size + (iz + 1) * size + (ix + 1)];
            float d011 = densities[(iy + 1) * size * size + (iz + 1) * size + ix];
            float d111 = densities[(iy + 1) * size * size + (iz + 1) * size + (ix + 1)];
            
            return Mathf.Lerp(
                Mathf.Lerp(Mathf.Lerp(d000, d100, fx), Mathf.Lerp(d010, d110, fx), fy),
                Mathf.Lerp(Mathf.Lerp(d001, d101, fx), Mathf.Lerp(d011, d111, fx), fy),
                fz
            );
        }
    }

    /// <summary>
    /// Container for mesh generation output
    /// </summary>
    public class MeshData
    {
        public List<Vector3> Vertices { get; } = new List<Vector3>(4096);
        public List<Vector3> Normals { get; } = new List<Vector3>(4096);
        public List<Vector2> UVs { get; } = new List<Vector2>(4096);
        public List<int> Triangles { get; } = new List<int>(6144);
        public List<VoxelType> TriangleMaterials { get; } = new List<VoxelType>(2048);

        public bool IsEmpty => Vertices.Count == 0;

        /// <summary>
        /// Applies generated mesh to a Unity Mesh object
        /// </summary>
        public void ApplyTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, UVs);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}