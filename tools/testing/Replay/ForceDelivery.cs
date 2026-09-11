using System;

namespace ArtOfSimRally.Testing
{
    internal static class ForceDelivery
    {
        public const string SingleProduct = "truncate-f32-product@1";
        public const string ExtendedProduct = "truncate-f64-product@1";
        // Float arithmetic has its own tolerance in replay. Delivery is exact
        // against the recorded output, using the observed runtime's conversion.
        public static int Quantise(float output, string contract)
        {
            if (contract == ExtendedProduct) return (int)((double)output * 10000);
            if (contract == SingleProduct) return (int)(output * 10000f);
            throw new ArgumentException("Unknown force delivery contract: " + contract);
        }
    }
}
