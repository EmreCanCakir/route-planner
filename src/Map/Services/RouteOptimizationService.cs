using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

namespace Map.Services
{
    public class RouteOptimizationService
    {
        public RouteOptimizationResult CalculateOptimalRoutes(List<List<int>> distanceMatrix, int vehicleCount, int depotIndex = 0)
        {
            var manager = new RoutingIndexManager(distanceMatrix.Count, vehicleCount, depotIndex);
            var routing = new RoutingModel(manager);
            
            // Maliyet (distance) fonksiyonu tanımla
            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                // Convert from routing variable Index to
                // distance matrix NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return distanceMatrix[fromNode][toNode];
            });

            // Define cost of each arc.
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Add Distance constraint.
            routing.AddDimension(transitCallbackIndex, 0, 150000,
                                 true, // start cumul to zero
                                 "Distance");
            RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
            distanceDimension.SetGlobalSpanCostCoefficient(100);

            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

            var solution = routing.SolveWithParameters(searchParameters);
            if (solution == null)
            {
                throw new Exception("Rota optimizasyonu için çözüm bulunamadı.");
            }
            //PrintSolution(vehicleCount, routing, manager, solution);
            return GetSolution(manager, routing, solution, vehicleCount);
        }

        static void PrintSolution(int vehicleCount, in RoutingModel routing, in RoutingIndexManager manager,
                                  in Assignment solution)
        {
            Console.WriteLine($"Objective {solution.ObjectiveValue()}:");

            // Inspect solution.
            long maxRouteDistance = 0;
            for (int i = 0; i < vehicleCount; ++i)
            {
                Console.WriteLine("Route for Vehicle {0}:", i);
                long routeDistance = 0;
                var index = routing.Start(i);
                while (routing.IsEnd(index) == false)
                {
                    Console.Write("{0} -> ", manager.IndexToNode((int)index));
                    var previousIndex = index;
                    index = solution.Value(routing.NextVar(index));
                    routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
                }
                Console.WriteLine("{0}", manager.IndexToNode((int)index));
                Console.WriteLine("Distance of the route: {0}m", routeDistance);
                maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
            }
            Console.WriteLine("Maximum distance of the routes: {0}m", maxRouteDistance);
        }

        private RouteOptimizationResult GetSolution(
         RoutingIndexManager manager,
         RoutingModel routing,
         Assignment solution,
         int vehicleCount)
        {
            var routes = new List<List<int>>();
            var routeDistances = new List<long>();

            for (int vehicleId = 0; vehicleId < vehicleCount; vehicleId++)
            {
                var route = new List<int>();
                long routeDistance = 0;

                for (var index = routing.Start(vehicleId); !routing.IsEnd(index); index = solution.Value(routing.NextVar(index)))
                {
                    var nodeIndex = manager.IndexToNode((int)index);
                    route.Add(nodeIndex);

                    // Toplam mesafeyi hesapla
                    var previousIndex = solution.Value(routing.NextVar(index));
                    if (!routing.IsEnd(previousIndex))
                    {
                        routeDistance += routing.GetArcCostForVehicle(index, previousIndex, vehicleId);
                    }
                }

                routes.Add(route);
                routeDistances.Add(routeDistance);
            }

            return new RouteOptimizationResult
            {
                Routes = routes,
                RouteDistances = routeDistances
            };
        }
        public class RouteOptimizationResult
        {
            public List<List<int>> Routes { get; set; }
            public List<long> RouteDistances { get; set; }
        }
    }
}