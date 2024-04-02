#define DELTA_TIME 0.01f
#define BETA 0.01f

// 姿勢(q)
float qx = 0.0f, qy = 0.0f, qz = 0.0f, qw = 1.0f;

inline float
inv_sqrt(float x) {
	long i = 0x5f3759df - (*(long*)&x >> 1);
	float half_x = 0.5f * x;
	float ret = *(float*)&i;
	ret = ret * (1.5f - (half_x * ret * ret));
	return ret * (1.5f - (half_x * ret * ret));
}

void update() {
	// 磁界の向き(m)
	float mx, my, mz;
	// 加速度(a)
	float ax, ay, az;
	// 角速度(ω)
	float wx, wy, wz;
	// 角速度(-32768～32768)を(-π～π)に変換
	wx = 9.5873799e-5f;
	wy = 9.5873799e-5f;
	wz = 9.5873799e-5f;
	// 回転量(Δq)
	float dqx, dqy, dqz, dqw;
	// 回転量＝姿勢(q)が角速度(w)で回転するときの時間変化
	// |  0 -wx -wy -wz || qx |
	// | wx   0  wz -wy || qy |
	// | wy -wz   0  wx || qz |
	// | wz  wy -wx   0 || qw |
	dqx = 0.499f*(      -wx*qy -wy*qz -wz*qw);
	dqy = 0.499f*(wx*qx        +wz*qz -wy*qw);
	dqz = 0.499f*(wy*qx -wz*qy        +wx*qw);
	dqw = 0.499f*(wz*qx +wy*qy -wx*qz       );
	if(!(0.0f == ax && 0.0f == ay && 0.0f == az)) {
		// 方位角の変化量(Δc)
		float dcx, dcy, dcz;
		// 方位角(c) - 磁界の向き(m)
		float c_mx, c_my, c_mz;
		// 重力の向き(g)
		float gx, gy, gz;
		{
			float qxx = qx*qx;
			float qxy = qx*qy;
			float qxz = qx*qz;
			float qxw = qx*qw;
			float qyy = qy*qy;
			float qyz = qy*qz;
			float qyw = qy*qw;
			float qzz = qz*qz;
			float qzw = qz*qw;
			float qww = qw*qw;
			// 磁界の向きを正規化
			float k = inv_sqrt(mx*mx + my*my + mz*mz);
			mx *= k;
			my *= k;
			mz *= k;
			float _2mx = 2.0f*mx;
			float _2my = 2.0f*my;
			float _2mz = 2.0f*mz;
			// 方位角の変化量(Δc)
			dcx = qxx*mx + qyy*mx - qzz*mx - qww*mx + qxz*_2mz - qxw*_2my + qyz*_2my + qyw*_2mz;
			dcy = qxx*my - qyy*my + qzz*my - qww*my - qxy*_2mz + qxw*_2mx + qyz*_2mx + qzw*_2mz;
			dcz = qxx*mz - qyy*mz - qzz*mz + qww*mz + qxy*_2my - qxz*_2mx + qyw*_2mx + qzw*_2my;
			dcx = dcx*dcx + dcy*dcy;
			dcx = dcx / inv_sqrt(dcx);
			// 方位角(c) - 磁界の向き(m)
			c_mx = dcx*(0.5f - qzz - qww) + dcz*(       qyw - qxz) - mx;
			c_my = dcx*(       qyz - qxw) + dcz*(       qxy + qzw) - my;
			c_mz = dcx*(       qxz + qyw) + dcz*(0.5f - qyy - qzz) - mz;
			// 加速度を正規化
			k = inv_sqrt(ax*ax + ay*ay + az*az);
			ax *= k;
			ay *= k;
			az *= k;
			// 重力の向き(g)
			gx =     2.0f*(qyw - qxz) - ax;
			gy =     2.0f*(qxy + qzw) - ay;
			gz = 1 - 2.0f*(qyy - qzz) - az;
		}
		// 加速度と磁界の向きを使用して勾配降下法による回転量の矯正を行う
		float gradx, grady, gradz, gradw;
		gradx =
			- 2.0f*qz*gx - (                    dcz*qz)*c_mx
			+ 2.0f*qy*gy + (-     dcx*qw +      dcz*qy)*c_my
			             + (      dcx*qz              )*c_mz;
		grady =
			  2.0f*qw*gx + (                    dcz*qw)*c_mx
			+ 2.0f*qx*gy + (      dcx*qz +      dcz*qx)*c_my
			- 4.0f*qy*gz + (      dcx*qw - 2.0f*dcz*qy)*c_mz;
		gradz =
			- 2.0f*qx*gx + (-2.0f*dcx*qz -      dcz*qx)*c_mx
			+ 2.0f*qw*gy + (      dcx*qy +      dcz*qw)*c_my
			- 4.0f*qz*gz + (      dcx*qx - 2.0f*dcz*qz)*c_mz;
		gradw =
			  2.0f*qy*gx + (-2.0f*dcx*qw +      dcz*qy)*c_mx
			+ 2.0f*qz*gy + (-     dcx*qx +      dcz*qz)*c_my
			             + (      dcx*qy              )*c_mz;
		// 勾配を正規化
		k = inv_sqrt(gradx*gradx + grady*grady + gradz*gradz + gradw*gradw);
		gradx *= k;
		grady *= k;
		gradz *= k;
		gradw *= k;
		// 勾配を回転量に反映
		dqx -= BETA * gradx;
		dqy -= BETA * grady;
		dqz -= BETA * gradz;
		dqw -= BETA * gradw;
	}
	// 回転量を積算して姿勢を更新
	qx += DELTA_TIME * dqx;
	qy += DELTA_TIME * dqy;
	qz += DELTA_TIME * dqz;
	qw += DELTA_TIME * dqw;
	// 姿勢を正規化
	float k = inv_sqrt(qx*qx + qy*qy + qz*qz + qw*qw);
	qx *= k;
	qy *= k;
	qz *= k;
	qw *= k;
}
