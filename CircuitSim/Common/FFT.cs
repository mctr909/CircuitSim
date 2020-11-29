using System;

namespace Circuit {
    class FFT {
        private int size;
        private int bits;
        private double[] cosTable;
        private double[] sinTable;

        public FFT(int n) {
            size = n;
            bits = (int)(Math.Log(size) / Math.Log(2));
            cosTable = new double[size / 2];
            sinTable = new double[size / 2];
            double dtheta = -2 * Math.PI / size;
            for (int i = 0; i < cosTable.Length; i++) {
                cosTable[i] = Math.Cos(dtheta * i);
                sinTable[i] = Math.Sin(dtheta * i);
            }
        }

        public void fft(double[] real, double[] imag) {
            int j = 0;
            int n2 = real.Length / 2;
            for (int i = 1; i < real.Length - 1; i++) {
                int n1 = n2;
                while (j >= n1) {
                    j -= n1;
                    n1 /= 2;
                }
                j += n1;
                if (i < j) {
                    double t1 = real[i];
                    real[i] = real[j];
                    real[j] = t1;
                    t1 = imag[i];
                    imag[i] = imag[j];
                    imag[j] = t1;
                }
            }
            n2 = 1;
            for (int i = 0; i < bits; i++) {
                int n1 = n2;
                n2 <<= 1;
                int a = 0;
                for (j = 0; j < n1; j++) {
                    double c = cosTable[a];
                    double s = sinTable[a];
                    a += 1 << (bits - i - 1);
                    for (int k = j; k < real.Length; k += n2) {
                        int t = k + n1;
                        double t1 = c * real[t] - s * imag[t];
                        double t2 = s * real[t] + c * imag[t];
                        real[k + n1] = real[k] - t1;
                        imag[k + n1] = imag[k] - t2;
                        real[k] += t1;
                        imag[k] += t2;
                    }
                }
            }
        }

        public int getSize() { return size; }

        public double magnitude(double real, double imag) {
            return Math.Sqrt(real * real + imag * imag) / size;
        }
    }
}
