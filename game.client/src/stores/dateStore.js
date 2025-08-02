import { ref } from 'vue';

// Global date store for synchronized date selection across components
const selectedDate = ref(new Date());

export const useDateStore = () => {
    const setSelectedDate = (date) => {
        selectedDate.value = date;
    };

    const setToday = () => {
        selectedDate.value = new Date();
    };

    const getSelectedDate = () => {
        return selectedDate.value;
    };

    const formatSelectedDate = () => {
        return selectedDate.value.toLocaleDateString('en-US', {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };

    return {
        selectedDate,
        setSelectedDate,
        setToday,
        getSelectedDate,
        formatSelectedDate
    };
};
