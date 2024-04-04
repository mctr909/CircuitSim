#include "gam_filter.h"
#include <math.h>

GAM_FILTER::GAM_FILTER() {
	delta_time = 1.0f / 100.0f;
	beta = 0.5f;
	qw = 1.0f;
	qx = 0.0f;
	qy = 0.0f;
	qz = 0.0f;
	hx = 0.0f;
	hy = 0.0f;
	hz = 0.0f;
	angles_computed = 0;
}

void GAM_FILTER::update(float wx, float wy, float wz, float ax, float ay, float az, float mx, float my, float mz) {
	// 角速度(-32768～32768)を(-π～π)に変換
	wx *= 9.5873799e-5f;
	wy *= 9.5873799e-5f;
	wz *= 9.5873799e-5f;
	// 回転量(Δq)＝姿勢(q)が角速度(w)で回転するときの時間変化
	float dqw, dqx, dqy, dqz;
	dqw = 0.5f*(         wx*qx + wy*qy + wz*qz);
	dqx = 0.5f*(-wx*qw         - wz*qy + wy*qz);
	dqy = 0.5f*(-wy*qw + wz*qx         - wx*qz);
	dqz = 0.5f*(-wz*qw - wy*qx + wx*qy        );
	if (!(0.0f == ax && 0.0f == ay && 0.0f == az)) {
		// 同じ計算を繰り返さないためにあらかじめ変数に置いておく
		float qwqw = qw*qw;
		float qwqx = qw*qx;
		float qwqy = qw*qy;
		float qwqz = qw*qz;
		float qxqx = qx*qx;
		float qxqy = qx*qy;
		float qxqz = qx*qz;
		float qyqy = qy*qy;
		float qyqz = qy*qz;
		float qzqz = qz*qz;
		// 補正勾配(s)＝重力の向きの勾配(grad g)＋姿勢方位の勾配(grad h) ⊗ 方位の変化量(Δm)
		float sw, sx, sy, sz;
		{
			// 加速度を正規化
			float r = 1.0f / sqrtf(ax*ax + ay*ay + az*az);
			ax *= r, ay *= r, az *= r;
			// 重力の向き(g)
			float gx, gy, gz;
			gx =     2*(qxqz - qwqy) - ax;
			gy =     2*(qwqx + qyqz) - ay;
			gz = 1 - 2*(qxqx + qyqy) - az;
			// 重力の向きの勾配(grad g)
			sw = -2*qy*gx + 2*qx*gy;
			sx =  2*qz*gx + 2*qw*gy - 4*qx*gz;
			sy = -2*qw*gx + 2*qz*gy - 4*qy*gz;
			sz =  2*qx*gx + 2*qy*gy;
		}
		{
			// 方位を正規化
			float r = 1.0f / sqrtf(mx*mx + my*my + mz*mz);
			mx *= r, my *= r, mz *= r;
			// 姿勢方位(h)＝方位(m)を姿勢(q)で回転させた向き
			float bx, bz;
			hx = 2*(qwqw + qxqx)*mx - mx + 2*(qxqy - qwqz)*my      + 2*(qxqz + qwqy)*mz;
			hy = 2*(qxqy + qwqz)*mx      + 2*(qwqw + qyqy)*my - my + 2*(qyqz - qwqx)*mz;
			hz = 2*(qxqz - qwqy)*mx      + 2*(qyqz + qwqx)*my      + 2*(qwqw + qzqz)*mz - mz;
			bx = sqrtf(hx*hx + hy*hy);
			bz = hz;
			hx = mx;
			hy = my;
			hz = mz;
			// 方位の変化量(Δm)
			float dmx, dmy, dmz;
			dmx = (0.5f - qyqy - qzqz)*bx + (       qxqz - qwqy)*bz - mx;
			dmy = (       qxqy - qwqz)*bx + (       qwqx + qyqz)*bz - my;
			dmz = (       qwqy + qxqz)*bx + (0.5f - qxqx - qyqy)*bz - mz;
			// 姿勢方位の勾配(grad h)⊗方位の変化量(Δm)
			sw += (         - qy*bz)*dmx + (-qz*bx + qx*bz)*dmy + (qy*bx          )*dmz;
			sx += (           qz*bz)*dmx + ( qy*bx + qw*bz)*dmy + (qz*bx - 2*qw*bz)*dmz;
			sy += (-2*qy*bx - qw*bz)*dmx + ( qx*bx + qz*bz)*dmy + (qw*bx - 2*qy*bz)*dmz;
			sz += (-2*qz*bx + qx*bz)*dmx + (-qw*bx + qy*bz)*dmy + (qx*bx          )*dmz;
		}
		// 補正勾配を正規化
		float r = 1.0f / sqrtf(sw*sw + sx*sx + sy*sy + sz*sz);
		sw *= r, sx *= r, sy *= r, sz *= r;
		// 回転量に補正勾配を反映
		dqw -= beta * sw;
		dqx -= beta * sx;
		dqy -= beta * sy;
		dqz -= beta * sz;
	}
	// 回転量を積算して姿勢を更新
	qw += dqw * delta_time;
	qx += dqx * delta_time;
	qy += dqy * delta_time;
	qz += dqz * delta_time;
	// 姿勢を正規化
	float r = 1.0f / sqrtf(qw*qw + qx*qx + qy*qy + qz*qz);
	qw *= r, qx *= r, qy *= r, qz *= r;
	angles_computed = 1;
}

void GAM_FILTER::compute_angles() {
	roll = -atan2f(qw*qx + qy*qz, 0.5f - qx*qx - qy*qy);
	pitch = asinf(-2*(qx*qz - qw*qy));
	yaw = atan2f(qw*qz + qx*qy, 0.5f - qy*qy - qz*qz);
	angles_computed = 0;
}
