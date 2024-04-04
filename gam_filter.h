#ifndef __GAM_FILTER_H__
#define __GAM_FILTER_H__

class GAM_FILTER{
private:
	float delta_time;
	float beta;
	float qw;
	float qx;
	float qy;
	float qz;
	float roll;
	float pitch;
	float yaw;
	char angles_computed;

public:
	float hx;
	float hy;
	float hz;

private:
	void compute_angles();

public:
	GAM_FILTER();
	void update(float wx, float wy, float wz, float ax, float ay, float az, float mx, float my, float mz);
	void set_sample_rate(float sample_frequency) {
		delta_time = 1.0f / sample_frequency;
	}
	void set_beta(float beta) {
		this->beta = beta;
	}
	float get_roll() {
		if (angles_computed) compute_angles();
		return roll;
	}
	float get_pitch() {
		if (angles_computed) compute_angles();
		return pitch;
	}
	float get_yaw() {
		if (angles_computed) compute_angles();
		return yaw;
	}
};

#endif
