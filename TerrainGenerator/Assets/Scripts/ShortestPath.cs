using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace {
    public class ShortestPath {
    
        public static float[,] FindShortestPath(float[,] heightMap, int[] start, int[] end) {
            int startX = start[0];
            int startY = start[0];
            
            int endX = end[0];
            int endY = end[0];
            
            int rows = heightMap.GetLength(0);
            int cols = heightMap.GetLength(1);

            float[,] shortestPath = new float[rows, cols];

            PriorityQueue<Point> queue = new PriorityQueue<Point>();
            queue.Enqueue(new Point(startX, startY, 0));

            bool[,] visited = new bool[rows, cols];

            // While the queue is not empty
            while (queue.Count() > 0) {
                
                // Dequeue the point with the lowest cost
                Point currentPoint = queue.Dequeue();
                // If the current point is the end point, we have found the shortest path
                if (currentPoint.X == endX && currentPoint.Y == endY)
                {
                    // Trace back the shortest path
                    Point tracePoint = currentPoint;
                    while (tracePoint.Previous != null)
                    {
                        shortestPath[tracePoint.X, tracePoint.Y] = 1;
                        tracePoint = tracePoint.Previous;
                    }
                    shortestPath[tracePoint.X, tracePoint.Y] = 1;
                    break;
                }

                // Mark the current point as visited
                visited[currentPoint.X, currentPoint.Y] = true;

                // Check the neighboring points
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        int x = currentPoint.X + i;
                        int y = currentPoint.Y + j;

                        if (x < 0 || x >= rows || y < 0 || y >= cols)
                            continue;

                        if (visited[x, y])
                            continue;

                        // Calculate the cost of going to this point
                        // float cost = /*heightMap[x,y] +*/ Distance(currentPoint.X, currentPoint.Y, x, y);
                        float distance = Distance(currentPoint.X,currentPoint.Y, x, y);
                        // float heightDiff = heightMap[x, y] - heightMap[currentPoint.X,currentPoint.Y] ;
                        float cost = heightMap[x,y] + distance;
                        
                        
                        // Add the point to the queue with the cost
                        queue.Enqueue(new Point(x, y, currentPoint.Cost + cost, currentPoint));
                    }
                }
            }

            return shortestPath;
        }

        static float Distance(int x1, int y1, int x2, int y2) {
            return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2);
        }


        class Point : IComparable<Point> {
            public int X { get; set; }
            public int Y { get; set; }
            public float Cost { get; set; }
            public Point Previous { get; set; }
            public Point(int x, int y, float cost, Point previous = null)
            {
                X = x;
                Y = y;
                Cost = cost;
                Previous = previous;
            }

            public int CompareTo(Point other)
            {
                return Cost.CompareTo(other.Cost);
            }
        }
    }
}