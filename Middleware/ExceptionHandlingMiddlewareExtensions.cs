namespace RouteWise.Middleware
{
    public static class ExceptionHandlingMiddlewareExtensions
    {
        /// <summary>
        /// Enables centralized exception handling within the ASP.NET Core application.
        /// </summary>
        /// <param name="builder">The application builder used to configure the request pipeline.</param>
        /// <returns>
        /// The updated <see cref="IApplicationBuilder"/> instance with the exception handling middleware added.
        /// </returns>
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
