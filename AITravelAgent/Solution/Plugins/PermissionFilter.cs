using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0001 
public class PermissionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
         if ((context.Function.PluginName == "FlightBooking" && context.Function.Name == "book_flight"))
        {
            Console.WriteLine("System Message: The agent requires an approval to complete this operation. Do you approve (Y/N)");
            Console.Write("User: ");
            string shouldProceed = Console.ReadLine()!;

            if (shouldProceed != "Y")
            {
                context.Result = new FunctionResult(context.Result, "The operation was not approved by the user");
                return;
            }
        }

        await next(context);
    }
}